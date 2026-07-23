using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.DataSources;

/// <summary>
/// A REAL, live data source: genuine Amazon.sa listings via Bright Data's
/// Web Scraper API (Amazon Products dataset, search-by-keyword collection) —
/// a third, independent Amazon source alongside DataForSeoAmazonDataSource
/// and OxylabsAmazonDataSource. Verified directly against the real API with
/// a real trial key: this dataset's "sa" domain genuinely works (returned a
/// real Arabic-titled listing with a SAR price, rating, and image) — unlike
/// OxylabsAmazonDataSource, whose "sa" domain fails on Oxylabs' own
/// infrastructure (see its remarks). Keeping all three active means one
/// vendor's outage, or one vendor's gap in Saudi coverage specifically,
/// doesn't remove Amazon from results.
///
/// Unlike every other source here, Bright Data's dataset API is
/// asynchronous: POST /trigger starts a collection job and returns a
/// snapshot_id, then GET /snapshot/{id} must be polled — confirmed via a
/// real trigger+poll cycle that it returns HTTP 202 with
/// {"status":"starting",...} while the job runs, then HTTP 200 with the
/// finished JSON array once ready (typically within a few seconds to ~30s
/// for this endpoint). That polling happens entirely inside SearchAsync so
/// this source still satisfies the synchronous IProductDataSource contract.
///
/// Config-only activation: dormant until DataSources:BrightData:ApiKey is
/// set. On Render this is DataSources__BrightData__ApiKey. Bright Data
/// authenticates with a single bearer token (unlike DataForSEO/Oxylabs'
/// login+password pairs) — the API key shown on their dashboard's Quick
/// start page.
/// </summary>
public sealed class BrightDataAmazonDataSource : IProductDataSource
{
    private const string TriggerEndpoint = "https://api.brightdata.com/datasets/v3/trigger";

    // One real, relevant offer per query — same reasoning as every other
    // Amazon source here.
    private const int MaxResults = 1;

    // Real observed timing: a search job finished in ~5s on one run. Poll
    // every 3s up to a ~40s total budget (matching DataForSeoAmazonDataSource's
    // timeout budget) so this source doesn't become the new slowest one in
    // the fan-out by a wide margin, while still giving genuinely slow jobs a
    // real chance to finish. Configurable (DataSources:BrightData:PollIntervalMs)
    // so tests can avoid a real multi-second wait.
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaxPollDuration = TimeSpan.FromSeconds(40);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BrightDataAmazonDataSource> _logger;
    private readonly string? _apiKey;
    private readonly string _datasetId;
    private readonly string _domain;
    private readonly TimeSpan _pollInterval;

    public BrightDataAmazonDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BrightDataAmazonDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["DataSources:BrightData:ApiKey"];
        // gd_lwdb4vjm1ehb499uxs = Bright Data's "Amazon Products Search
        // (collect by URL)" dataset — confirmed via their own API reference
        // and a real trigger+poll call against it.
        _datasetId = configuration["DataSources:BrightData:DatasetId"] ?? "gd_lwdb4vjm1ehb499uxs";
        _domain = configuration["DataSources:BrightData:Domain"] ?? "sa";
        _pollInterval = double.TryParse(configuration["DataSources:BrightData:PollIntervalMs"], out var ms)
            ? TimeSpan.FromMilliseconds(ms)
            : DefaultPollInterval;
    }

    public string SourceName => "BrightDataAmazon";

    /// <summary>Active only once an API key is configured; otherwise fully dormant.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiKey);

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
            // DataForSeoAmazonDataSource, OxylabsAmazonDataSource) — an untranslated
            // Arabic category word matches nothing in Amazon's own listings.
            var effectiveQuery = ArabicBrandNormalizer.Normalize(query);

            var client = _httpClientFactory.CreateClient(nameof(BrightDataAmazonDataSource));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var snapshotId = await TriggerSearchAsync(client, effectiveQuery, cancellationToken);
            if (snapshotId is null)
            {
                return Array.Empty<StoreOffer>();
            }

            var items = await PollForResultsAsync(client, snapshotId, query, cancellationToken);
            return MapToOffers(items);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail soft (NFR4): a bad response must not break the whole search.
            _logger.LogWarning(ex, "Bright Data Amazon source failed for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
    }

    private async Task<string?> TriggerSearchAsync(HttpClient client, string effectiveQuery, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{TriggerEndpoint}?dataset_id={_datasetId}&include_errors=true")
        {
            Content = JsonContent.Create(new object[]
            {
                new { keyword = effectiveQuery, url = $"https://www.amazon.{_domain}", pages_to_search = 1 },
            }),
        };

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Bright Data Amazon trigger failed with {StatusCode} for {Query}", response.StatusCode, effectiveQuery);
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<TriggerResponse>(cancellationToken: cancellationToken);
        return body?.SnapshotId;
    }

    private async Task<List<BrightDataItem>> PollForResultsAsync(
        HttpClient client, string snapshotId, string query, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + MaxPollDuration;
        var url = $"https://api.brightdata.com/datasets/v3/snapshot/{snapshotId}?format=json";

        while (true)
        {
            using var response = await client.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadFromJsonAsync<List<BrightDataItem>>(cancellationToken: cancellationToken) ?? [];
            }

            // 202 = still running (their documented "not ready yet" shape);
            // anything else is a genuine error worth giving up on immediately
            // rather than polling a job that will never complete.
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                _logger.LogWarning(
                    "Bright Data Amazon snapshot poll failed with {StatusCode} for {Query}", response.StatusCode, query);
                return [];
            }

            if (DateTime.UtcNow >= deadline)
            {
                _logger.LogWarning(
                    "Bright Data Amazon snapshot for {Query} did not finish within {Timeout}", query, MaxPollDuration);
                return [];
            }

            await Task.Delay(_pollInterval, cancellationToken);
        }
    }

    private static List<StoreOffer> MapToOffers(List<BrightDataItem> items)
    {
        var offers = new List<StoreOffer>();
        foreach (var item in items)
        {
            if (item.FinalPrice is not { } price || price <= 0
                || string.IsNullOrWhiteSpace(item.Url)
                || string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            offers.Add(new StoreOffer
            {
                StoreName = "Amazon.sa",
                ProductTitle = item.Name!,
                Price = price,
                Currency = item.Currency ?? "SAR",
                ProductUrl = item.Url!,
                InStock = true,
                ImageUrl = item.Image,
                FreeShipping = false,
                DeliveryDays = null,
                Rating = item.Rating is { } r ? (decimal)r : null,
                RatingCount = item.NumRatings,
            });

            if (offers.Count >= MaxResults)
            {
                break;
            }
        }

        return offers;
    }

    private sealed record TriggerResponse([property: JsonPropertyName("snapshot_id")] string? SnapshotId);

    private sealed record BrightDataItem(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("final_price")] decimal? FinalPrice,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("image")] string? Image,
        [property: JsonPropertyName("rating")] double? Rating,
        [property: JsonPropertyName("num_ratings")] int? NumRatings);
}
