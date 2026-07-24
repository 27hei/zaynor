using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.DataSources;

/// <summary>
/// A REAL, live data source: genuine Amazon.sa listings (real current prices,
/// product images, direct product URLs) via the Rainforest API aggregator
/// (https://rainforestapi.com). This is the fastest path to real data at scale
/// — no per-product manual entry — since one API key unlocks the whole Amazon
/// catalogue. The outbound /api/out redirect then appends our Associates tag to
/// each product link, so clicks stay monetized.
///
/// Config-only activation (matching the deeplink infrastructure): the source is
/// dormant and contributes nothing until DataSources:Rainforest:ApiKey is set,
/// so shipping this code changes nothing until a key exists. On Render the key
/// is the environment variable DataSources__Rainforest__ApiKey.
/// </summary>
public sealed class RainforestAmazonDataSource : IProductDataSource
{
    private const string Endpoint = "https://api.rainforestapi.com/request";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RainforestAmazonDataSource> _logger;
    private readonly string? _apiKey;
    private readonly string _amazonDomain;
    private readonly int _maxResults;

    public RainforestAmazonDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RainforestAmazonDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["DataSources:Rainforest:ApiKey"];
        _amazonDomain = configuration["DataSources:Rainforest:AmazonDomain"] ?? "amazon.sa";
        // A store can now show more than one genuinely different listing per
        // search — but not unbounded (this is a paid, per-request API), so a
        // shared, config-driven cap across all single-call sources.
        _maxResults = int.TryParse(configuration["DataSources:MaxListingsPerSource"], out var max) ? max : 10;
    }

    public string SourceName => "RainforestAmazon";

    /// <summary>Active only once an API key is configured; otherwise fully dormant.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>Trial/paid API quota — queried on every search, alongside the curated catalog and other live feeds.</summary>
    public bool IsExpensiveLive => true;

    /// <summary>A direct Amazon.sa scraper, same tier as the other Amazon sources.</summary>
    public double Confidence => 0.85;

    public async Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Array.Empty<StoreOffer>();
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(RainforestAmazonDataSource));
            var url = $"{Endpoint}?api_key={Uri.EscapeDataString(_apiKey!)}"
                + $"&type=search&amazon_domain={Uri.EscapeDataString(_amazonDomain)}"
                + $"&search_term={Uri.EscapeDataString(query)}";

            var response = await client.GetFromJsonAsync<RainforestResponse>(url, cancellationToken);

            var offers = new List<StoreOffer>();
            foreach (var result in response?.SearchResults ?? [])
            {
                if (result.Price?.Value is not { } price || price <= 0
                    || string.IsNullOrWhiteSpace(result.Link)
                    || string.IsNullOrWhiteSpace(result.Title)
                    // Now that more than one listing per query can survive,
                    // apply the same precision filters GoogleShoppingDataSource
                    // needed once it stopped taking just a single top result —
                    // otherwise accessories/repair-parts matching the model
                    // name would surface as if they were the product itself.
                    || !ListingRelevanceFilter.IsRelevant(query, result.Title)
                    || ListingRelevanceFilter.IsAccessoryMismatch(query, query, result.Title))
                {
                    continue; // skip sponsored/price-less/off-topic rows — we only show buyable, relevant offers
                }

                offers.Add(new StoreOffer
                {
                    StoreName = "Amazon.sa",
                    ProductTitle = result.Title!,
                    Price = price,
                    Currency = result.Price.Currency ?? "SAR",
                    ProductUrl = result.Link!,
                    InStock = true,
                    ImageUrl = result.Image,
                    FreeShipping = false,
                    DeliveryDays = null,
                    ExternalId = result.Asin,
                });

                if (offers.Count >= _maxResults)
                {
                    break;
                }
            }

            return ListingRelevanceFilter.RemovePriceOutliers(offers);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail soft (NFR4): a bad response must not break the whole search.
            _logger.LogWarning(ex, "Rainforest source failed for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
    }

    private sealed record RainforestResponse(
        [property: JsonPropertyName("search_results")] List<RainforestResult>? SearchResults);

    private sealed record RainforestResult(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("link")] string? Link,
        [property: JsonPropertyName("image")] string? Image,
        [property: JsonPropertyName("asin")] string? Asin,
        [property: JsonPropertyName("price")] RainforestPrice? Price);

    private sealed record RainforestPrice(
        [property: JsonPropertyName("value")] decimal? Value,
        [property: JsonPropertyName("currency")] string? Currency);
}
