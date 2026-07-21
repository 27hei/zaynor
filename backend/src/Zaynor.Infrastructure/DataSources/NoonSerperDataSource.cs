using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.DataSources;

/// <summary>
/// A REAL, live data source: Noon.com prices/images sourced from Google
/// Shopping via Serper (serper.dev), since Noon has no official product
/// API and blocks direct/automated requests to its own site (confirmed by
/// hand: every direct fetch attempt — search page, product page, short
/// links — failed from this hosting environment; Google's own crawler is
/// specifically trusted, and Serper queries Google, not Noon, so it never
/// hits that wall).
///
/// Google's shopping "link" field points to a Google compare-prices page,
/// not directly to Noon, and wouldn't carry our affiliate tag if used
/// as-is — so the outbound URL is instead a Noon site-search for the exact
/// title Google returned (same mechanism as the NoonFallbackLink UI
/// component), which rides through /api/out and gets tagged automatically.
///
/// Config-only activation: dormant until DataSources:Serper:ApiKey is set
/// (env: DataSources__Serper__ApiKey). Billed per request by Serper (very
/// cheap: ~$1/1,000 after a 2,500-request free allowance), so — like the
/// other live feeds — only called when the curated catalog has no match.
/// </summary>
public sealed class NoonSerperDataSource : IProductDataSource
{
    private const string Endpoint = "https://google.serper.dev/shopping";

    // Google Shopping returns many sellers per product; only the single
    // best-ranked Noon listing is surfaced — several listings aren't the
    // same product compared across stores, they're just search results.
    private const int MaxResults = 1;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NoonSerperDataSource> _logger;
    private readonly string? _apiKey;

    public NoonSerperDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<NoonSerperDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["DataSources:Serper:ApiKey"];
    }

    public string SourceName => "NoonSerper";

    /// <summary>Active only once a Serper API key is configured; otherwise fully dormant.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>Paid-per-request API — only called when the curated catalog has no match.</summary>
    public bool IsExpensiveLive => true;

    public async Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Array.Empty<StoreOffer>();
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(NoonSerperDataSource));
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(new { q = query, gl = "sa", hl = "en" }),
            };
            request.Headers.Add("X-API-KEY", _apiKey);

            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var envelope = await response.Content.ReadFromJsonAsync<SerperEnvelope>(cancellationToken: cancellationToken);
            var items = envelope?.Shopping ?? [];

            var offers = new List<StoreOffer>();
            foreach (var item in items)
            {
                if (!IsNoon(item.Source)
                    || string.IsNullOrWhiteSpace(item.Title)
                    || ParsePrice(item.Price) is not { } price || price <= 0)
                {
                    continue;
                }

                // Google's link goes to a Google compare-prices page, not Noon
                // directly — send the click to a Noon search for this exact
                // title instead, so /api/out can tag it (see class remarks).
                var noonSearchUrl = $"https://www.noon.com/saudi-en/search/?q={Uri.EscapeDataString(item.Title!)}";

                offers.Add(new StoreOffer
                {
                    StoreName = "Noon",
                    ProductTitle = item.Title!,
                    Price = price,
                    Currency = "SAR",
                    ProductUrl = noonSearchUrl,
                    InStock = true,
                    ImageUrl = item.ImageUrl,
                    FreeShipping = false,
                    DeliveryDays = null,
                });

                if (offers.Count >= MaxResults)
                {
                    break;
                }
            }

            return offers;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "NoonSerper source failed for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
    }

    private static bool IsNoon(string? source) =>
        !string.IsNullOrWhiteSpace(source) && source.Contains("noon", StringComparison.OrdinalIgnoreCase);

    /// <summary>Serper's price is a "SAR 2,699.00"-style string — parse leniently.</summary>
    private static decimal? ParsePrice(string? priceRaw)
    {
        if (string.IsNullOrWhiteSpace(priceRaw))
        {
            return null;
        }

        var digits = new string(priceRaw.Where(c => char.IsDigit(c) || c == '.').ToArray());
        return decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private sealed record SerperEnvelope(
        [property: JsonPropertyName("shopping")] List<SerperShoppingItem>? Shopping);

    private sealed record SerperShoppingItem(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("source")] string? Source,
        [property: JsonPropertyName("price")] string? Price,
        [property: JsonPropertyName("imageUrl")] string? ImageUrl);
}
