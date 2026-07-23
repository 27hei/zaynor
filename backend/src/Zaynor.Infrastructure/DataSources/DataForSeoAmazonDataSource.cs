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
/// A REAL, live data source: genuine Amazon.sa listings via DataForSEO's
/// Merchant API (Amazon Products, live/advanced endpoint) — an alternative to
/// RainforestAmazonDataSource covering the same need (guaranteed Amazon
/// coverage on every search, not just when Google Shopping happens to
/// surface it). The outbound /api/out redirect appends our Associates tag to
/// each product link, so clicks stay monetized regardless of which source
/// found the offer.
///
/// Config-only activation (matching the rest of DataSources): dormant until
/// both DataSources:DataForSeo:Login and DataSources:DataForSeo:Password are
/// set. On Render these are DataSources__DataForSeo__Login /
/// DataSources__DataForSeo__Password. DataForSEO authenticates with HTTP
/// Basic auth using the account email + API password from the API Access
/// dashboard page — not a bearer token/single API key like the other
/// sources, hence the two separate config values.
/// </summary>
public sealed class DataForSeoAmazonDataSource : IProductDataSource
{
    private const string Endpoint = "https://api.dataforseo.com/v3/merchant/amazon/products/live/advanced";

    // A live search for one keyword returns many distinct listings (different
    // sellers/bundles/accessories) — taking more than the single best match
    // would show them as if they were different stores' prices for the same
    // product, which they aren't. One real, relevant offer per query, same
    // reasoning as RainforestAmazonDataSource.
    private const int MaxResults = 1;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DataForSeoAmazonDataSource> _logger;
    private readonly string? _login;
    private readonly string? _password;
    private readonly string _locationName;
    private readonly string _languageName;

    public DataForSeoAmazonDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DataForSeoAmazonDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _login = configuration["DataSources:DataForSeo:Login"];
        _password = configuration["DataSources:DataForSeo:Password"];
        _locationName = configuration["DataSources:DataForSeo:LocationName"] ?? "Saudi Arabia";
        // "Arabic"/"Arabic (Saudi Arabia)" are rejected as invalid by this
        // endpoint (confirmed against the live API) — "English (United
        // States)" is the documented, verified-working value and still
        // returns the same amazon.sa marketplace/currency/listings.
        _languageName = configuration["DataSources:DataForSeo:LanguageName"] ?? "English (United States)";
    }

    public string SourceName => "DataForSeoAmazon";

    /// <summary>Active only once login+password are configured; otherwise fully dormant.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_login) && !string.IsNullOrWhiteSpace(_password);

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
            // A real observed failure: a bare Arabic category word ("نظارة")
            // returned nothing, because it was sent to DataForSEO as-is —
            // this endpoint's own results skew English/international-brand
            // titled, so the untranslated Arabic keyword matches nothing.
            // Same normalization GoogleShoppingDataSource applies itself.
            var effectiveQuery = ArabicBrandNormalizer.Normalize(query);

            var client = _httpClientFactory.CreateClient(nameof(DataForSeoAmazonDataSource));
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(new object[]
                {
                    new { keyword = effectiveQuery, location_name = _locationName, language_name = _languageName },
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_login}:{_password}")));

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadFromJsonAsync<DataForSeoResponse>(cancellationToken: cancellationToken);

            var task = body?.Tasks?.FirstOrDefault();
            var offers = new List<StoreOffer>();
            var items = task?.Result?.FirstOrDefault()?.Items ?? [];

            // DataForSEO wraps most errors (insufficient balance, rate limits,
            // invalid params) inside an HTTP 200 with a non-20000 task-level
            // status_code rather than a 4xx/5xx — a real observed bug: this
            // source silently returned zero offers for queries DataForSEO
            // genuinely had data for, because a fast, no-cost per-task error
            // response looks identical to "no matches" without logging these
            // fields. Logged whenever there are no items so a config/billing
            // problem shows up in production logs immediately instead of
            // requiring a manual API call to diagnose.
            if (items.Count == 0)
            {
                _logger.LogWarning(
                    "DataForSEO Amazon returned no items for {Query}: top-level {TopCode} {TopMessage}; task {TaskCode} {TaskMessage} (cost {Cost})",
                    query, body?.StatusCode, body?.StatusMessage, task?.StatusCode, task?.StatusMessage, task?.Cost);
            }
            foreach (var item in items)
            {
                // "amazon_paid" (and other non-"amazon_serp" rows) are sponsored
                // placements or unrelated blocks (e.g. related_searches) — only
                // organic listings are real, comparable offers.
                if (item.Type != "amazon_serp"
                    || item.PriceFrom is not { } price || price <= 0
                    || string.IsNullOrWhiteSpace(item.Url)
                    || string.IsNullOrWhiteSpace(item.Title))
                {
                    continue;
                }

                offers.Add(new StoreOffer
                {
                    StoreName = "Amazon.sa",
                    ProductTitle = item.Title!,
                    Price = price,
                    Currency = item.Currency ?? "SAR",
                    ProductUrl = item.Url!,
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
            // Fail soft (NFR4): a bad response must not break the whole search.
            _logger.LogWarning(ex, "DataForSEO Amazon source failed for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
    }

    private sealed record DataForSeoResponse(
        [property: JsonPropertyName("status_code")] int? StatusCode,
        [property: JsonPropertyName("status_message")] string? StatusMessage,
        [property: JsonPropertyName("tasks")] List<DataForSeoTask>? Tasks);

    private sealed record DataForSeoTask(
        [property: JsonPropertyName("status_code")] int? StatusCode,
        [property: JsonPropertyName("status_message")] string? StatusMessage,
        [property: JsonPropertyName("cost")] decimal? Cost,
        [property: JsonPropertyName("result")] List<DataForSeoResult>? Result);

    private sealed record DataForSeoResult([property: JsonPropertyName("items")] List<DataForSeoItem>? Items);

    private sealed record DataForSeoItem(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("image_url")] string? ImageUrl,
        [property: JsonPropertyName("price_from")] decimal? PriceFrom,
        [property: JsonPropertyName("currency")] string? Currency);
}
