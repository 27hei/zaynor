using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.DataSources;

/// <summary>
/// A REAL, live data source: genuine Amazon.sa listings via Oxylabs' Web
/// Scraper API (Amazon Search, real-time endpoint) — a second, independent
/// Amazon source alongside RainforestAmazonDataSource and
/// DataForSeoAmazonDataSource. Deliberately redundant: a real incident during
/// this project's development (DataForSEO's own anti-fraud system paused the
/// account without warning) showed that depending on a single vendor for
/// Amazon coverage is a real production risk, not a hypothetical one — with
/// two independent vendors active at once, one vendor's outage no longer
/// means Amazon disappears from search results entirely.
///
/// Config-only activation (matching the rest of DataSources): dormant until
/// both DataSources:Oxylabs:Username and DataSources:Oxylabs:Password are
/// set. On Render these are DataSources__Oxylabs__Username /
/// DataSources__Oxylabs__Password — Oxylabs authenticates with HTTP Basic
/// auth using the API user credentials created in their dashboard (separate
/// from the account login), the same shape as DataForSEO's login+password.
/// </summary>
public sealed class OxylabsAmazonDataSource : IProductDataSource
{
    private const string Endpoint = "https://realtime.oxylabs.io/v1/queries";

    // Same reasoning as every other Amazon source here: one real, relevant
    // offer per query, not every seller/bundle/accessory variant.
    private const int MaxResults = 1;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OxylabsAmazonDataSource> _logger;
    private readonly string? _username;
    private readonly string? _password;
    private readonly string _domain;
    private readonly string _geoLocation;
    private readonly string _currency;

    public OxylabsAmazonDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OxylabsAmazonDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _username = configuration["DataSources:Oxylabs:Username"];
        _password = configuration["DataSources:Oxylabs:Password"];
        // "sa" selects the amazon.sa marketplace directly (confirmed listed
        // in Oxylabs' supported Amazon domains) — not a generic .com search
        // with a geo hint, which would return US listings/pricing.
        _domain = configuration["DataSources:Oxylabs:Domain"] ?? "sa";
        _geoLocation = configuration["DataSources:Oxylabs:GeoLocation"] ?? "Saudi Arabia";
        _currency = configuration["DataSources:Oxylabs:Currency"] ?? "SAR";
    }

    public string SourceName => "OxylabsAmazon";

    /// <summary>Active only once username+password are configured; otherwise fully dormant.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password);

    /// <summary>Paid per-request quota — queried on every search, alongside the curated catalog and other live feeds.</summary>
    public bool IsExpensiveLive => true;

    public async Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Array.Empty<StoreOffer>();
        }

        try
        {
            // Same normalization every other source applies (GoogleShoppingDataSource,
            // DataForSeoAmazonDataSource) — an untranslated Arabic category word
            // matches nothing in Amazon's own English-skewed listings.
            var effectiveQuery = ArabicBrandNormalizer.Normalize(query);

            var client = _httpClientFactory.CreateClient(nameof(OxylabsAmazonDataSource));
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(new
                {
                    source = "amazon_search",
                    domain = _domain,
                    query = effectiveQuery,
                    pages = 1,
                    parse = true,
                    geo_location = _geoLocation,
                    context = new[] { new { key = "currency", value = _currency } },
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}")));

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<OxylabsResponse>(cancellationToken: cancellationToken);

            var offers = new List<StoreOffer>();
            var organic = body?.Results?.FirstOrDefault()?.Content?.Results?.Organic ?? [];

            if (organic.Count == 0)
            {
                _logger.LogWarning("Oxylabs Amazon returned no organic results for {Query}", query);
            }

            foreach (var item in organic)
            {
                if (item.Price is not { } price || price <= 0
                    || string.IsNullOrWhiteSpace(item.Url)
                    || string.IsNullOrWhiteSpace(item.Title))
                {
                    continue;
                }

                // Oxylabs returns a path (e.g. "/dp/B0CM2QJ9T5/...") rather
                // than a full URL — resolve it against the real marketplace
                // domain we asked for, so the outbound link actually works.
                var productUrl = item.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? item.Url
                    : $"https://www.amazon.{_domain}{(item.Url.StartsWith('/') ? "" : "/")}{item.Url}";

                offers.Add(new StoreOffer
                {
                    StoreName = "Amazon.sa",
                    ProductTitle = item.Title!,
                    Price = price,
                    Currency = item.Currency ?? _currency,
                    ProductUrl = productUrl,
                    InStock = true,
                    ImageUrl = item.UrlImage,
                    FreeShipping = false,
                    DeliveryDays = null,
                    Rating = item.Rating is { } r ? (decimal)r : null,
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
            // Fail soft (NFR4): a bad response must not break the whole search.
            _logger.LogWarning(ex, "Oxylabs Amazon source failed for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
    }

    private sealed record OxylabsResponse([property: JsonPropertyName("results")] List<OxylabsResult>? Results);

    private sealed record OxylabsResult([property: JsonPropertyName("content")] OxylabsContent? Content);

    private sealed record OxylabsContent([property: JsonPropertyName("results")] OxylabsInnerResults? Results);

    private sealed record OxylabsInnerResults([property: JsonPropertyName("organic")] List<OxylabsItem>? Organic);

    private sealed record OxylabsItem(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("price")] decimal? Price,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("url_image")] string? UrlImage,
        [property: JsonPropertyName("asin")] string? Asin,
        [property: JsonPropertyName("rating")] double? Rating);
}
