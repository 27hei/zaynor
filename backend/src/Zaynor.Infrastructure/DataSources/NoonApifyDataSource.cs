using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.DataSources;

/// <summary>
/// A REAL, live data source: Noon.com search results (current prices, images,
/// direct product URLs) via a third-party scraping actor hosted on Apify
/// (apify.com/powerai/noon-products-search-scraper), since Noon does not
/// publish an official product-search API. Outbound clicks are tagged
/// separately by OutController's Affiliate:NoonUtmSuffix — this source is
/// only responsible for finding real Noon offers to show.
///
/// Caveat (surfaced to the founder, not hidden): this is unofficial scraping,
/// not a sanctioned partner API — it can break without notice and may run
/// against Noon's terms of service. Config-only activation means it stays
/// fully dormant, and thus zero-risk, until a deliberate choice is made to
/// set an API token.
///
/// Config-only activation: dormant until DataSources:NoonApify:ApiToken is
/// set (env: DataSources__NoonApify__ApiToken). Billed per result by Apify,
/// so — like the other live feeds — only called when the curated catalog has
/// no match (spec: conserve quota-limited API budgets).
/// </summary>
public sealed class NoonApifyDataSource : IProductDataSource
{
    private const string ActorRunEndpoint =
        "https://api.apify.com/v2/acts/powerai~noon-products-search-scraper/run-sync-get-dataset-items";

    // The actor returns a ranked search results page; only the single best
    // match is surfaced — several listings from one search aren't the same
    // product compared across stores, they're just search results.
    private const int MaxResults = 1;

    // The actor rejects maxItems below 50 (server-side validation); this is
    // a request cap, not a fixed charge — Apify bills per item the run
    // actually returns, not per item requested.
    private const int RequestedItems = 50;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(25);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NoonApifyDataSource> _logger;
    private readonly string? _apiToken;

    public NoonApifyDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<NoonApifyDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiToken = configuration["DataSources:NoonApify:ApiToken"];
    }

    public string SourceName => "NoonApify";

    /// <summary>Active only once an Apify API token is configured; otherwise fully dormant.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiToken);

    /// <summary>Paid-per-result API — only called when the curated catalog has no match.</summary>
    public bool IsExpensiveLive => true;

    public async Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Array.Empty<StoreOffer>();
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(RequestTimeout);

            var searchUrl = $"https://www.noon.com/saudi-en/search/?q={Uri.EscapeDataString(query)}";
            var requestUrl = $"{ActorRunEndpoint}?token={Uri.EscapeDataString(_apiToken!)}";

            var client = _httpClientFactory.CreateClient(nameof(NoonApifyDataSource));
            var response = await client.PostAsJsonAsync(
                requestUrl,
                new { searchUrl, maxItems = RequestedItems },
                timeoutCts.Token);
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<List<NoonApifyProduct>>(
                cancellationToken: timeoutCts.Token) ?? [];

            var offers = new List<StoreOffer>();
            foreach (var item in items)
            {
                var price = ParsePrice(item.Price);
                var url = NormalizeUrl(item.ProductUrl);

                if (string.IsNullOrWhiteSpace(item.ProductName)
                    || price is not { } validPrice || validPrice <= 0
                    || url is null)
                {
                    continue;
                }

                offers.Add(new StoreOffer
                {
                    StoreName = "Noon",
                    ProductTitle = item.ProductName!,
                    Price = validPrice,
                    Currency = "SAR",
                    ProductUrl = url,
                    InStock = true,
                    ImageUrl = item.ImageUrl,
                    FreeShipping = item.IsExpress ?? false,
                    DeliveryDays = null,
                });

                if (offers.Count >= MaxResults)
                {
                    break;
                }
            }

            return offers;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Our own timeout fired (not the caller's token) — fail soft (NFR4).
            _logger.LogWarning("NoonApify source timed out for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "NoonApify source failed for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
    }

    /// <summary>The actor's price field shape isn't guaranteed (number or "SAR 229.00"-style string) — parse leniently.</summary>
    private static decimal? ParsePrice(JsonElement? price)
    {
        if (price is not { } value)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numeric))
        {
            return numeric;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var digits = new string((value.GetString() ?? string.Empty)
                .Where(c => char.IsDigit(c) || c == '.').ToArray());
            if (decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url
            : "https://www.noon.com" + (url.StartsWith('/') ? url : "/" + url);
    }

    private sealed record NoonApifyProduct(
        [property: JsonPropertyName("productName")] string? ProductName,
        [property: JsonPropertyName("price")] JsonElement? Price,
        [property: JsonPropertyName("imageUrl")] string? ImageUrl,
        [property: JsonPropertyName("productUrl")] string? ProductUrl,
        [property: JsonPropertyName("isExpress")] bool? IsExpress);
}
