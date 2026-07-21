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
/// A REAL, live data source: prices/images across every merchant Google
/// Shopping tracks for a query, via Serper (serper.dev) — not just Noon.
/// Merchants feed Google Shopping directly (Merchant Center), so this works
/// even for stores whose own sites block direct/automated requests (Noon
/// confirmed by hand: every direct fetch attempt failed from this hosting
/// environment; Google's own crawler is specifically trusted, and Serper
/// queries Google, not the merchant, so it never hits that wall).
///
/// Deliberately unfiltered by trust/reputation (spec: founder's call — the
/// user asked for every merchant Google returns, not just vetted ones).
/// Noon gets special handling because we can build a taggable outbound URL
/// for it (a Noon site-search, tagged by /api/out); every other merchant's
/// link is Google's own compare-prices page, since we have no way to
/// construct or verify that merchant's real site URL.
///
/// Config-only activation: dormant until DataSources:Serper:ApiKey is set
/// (env: DataSources__Serper__ApiKey). Billed per request by Serper (very
/// cheap: ~$1/1,000 after a 2,500-request free allowance), so — like the
/// other live feeds — only called when the curated catalog has no match.
/// </summary>
public sealed class GoogleShoppingDataSource : IProductDataSource
{
    private const string Endpoint = "https://google.serper.dev/shopping";

    // A generous cap, not the single-best-match philosophy used elsewhere —
    // the point here is breadth across many real merchants.
    private const int MaxResults = 30;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleShoppingDataSource> _logger;
    private readonly string? _apiKey;

    public GoogleShoppingDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GoogleShoppingDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["DataSources:Serper:ApiKey"];
    }

    public string SourceName => "GoogleShopping";

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
            var client = _httpClientFactory.CreateClient(nameof(GoogleShoppingDataSource));
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(new { q = query, gl = "sa", hl = "en" }),
            };
            request.Headers.Add("X-API-KEY", _apiKey);

            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var envelope = await response.Content.ReadFromJsonAsync<SerperEnvelope>(cancellationToken: cancellationToken);
            var items = envelope?.Shopping ?? [];

            // One offer per merchant (cheapest listing) — several listings
            // from the same store aren't different stores to compare.
            var bestPerMerchant = new Dictionary<string, StoreOffer>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Title)
                    || string.IsNullOrWhiteSpace(item.Source)
                    || ParsePrice(item.Price) is not { } price || price <= 0)
                {
                    continue;
                }

                var isNoon = IsNoon(item.Source);
                var storeName = isNoon ? "Noon" : item.Source!;

                // Noon: build our own taggable site-search URL (see class
                // remarks). Everyone else: Google's compare-prices link is
                // the only URL we have for them.
                var productUrl = isNoon
                    ? $"https://www.noon.com/saudi-en/search/?q={Uri.EscapeDataString(item.Title!)}"
                    : item.Link;

                if (string.IsNullOrWhiteSpace(productUrl))
                {
                    continue;
                }

                if (!bestPerMerchant.TryGetValue(storeName, out var existing) || price < existing.Price)
                {
                    bestPerMerchant[storeName] = new StoreOffer
                    {
                        StoreName = storeName,
                        ProductTitle = item.Title!,
                        Price = price,
                        Currency = "SAR",
                        ProductUrl = productUrl,
                        InStock = true,
                        ImageUrl = item.ImageUrl,
                        FreeShipping = false,
                        DeliveryDays = null,
                    };
                }
            }

            return bestPerMerchant.Values.Take(MaxResults).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "GoogleShopping source failed for query {Query}; skipping it", query);
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
        [property: JsonPropertyName("imageUrl")] string? ImageUrl,
        [property: JsonPropertyName("link")] string? Link);
}
