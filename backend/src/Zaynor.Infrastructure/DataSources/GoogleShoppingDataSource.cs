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
/// cheap: ~$1/1,000 after a 2,500-request free allowance). Queried on every
/// search alongside the curated catalog — max store coverage matters more
/// here than conserving quota (spec: founder's call).
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

    /// <summary>Paid-per-request API — queried on every search, alongside the curated catalog and other live feeds.</summary>
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
                    || ParsePrice(item.Price) is not { } price || price <= 0
                    || !IsRelevant(query, item.Title)
                    || IsAccessoryMismatch(query, item.Title))
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
                        Rating = item.Rating is { } r ? (decimal)r : null,
                        RatingCount = item.RatingCount,
                    };
                }
            }

            return RemovePriceOutliers(bestPerMerchant.Values.ToList()).Take(MaxResults).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "GoogleShopping source failed for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
    }

    /// <summary>
    /// A real observed gap in the keyword filters above: some listings
    /// (a mislabeled/deceptive accessory, or a case brand whose product name
    /// doesn't mention "case") carry no accessory keyword at all, yet price
    /// at a small fraction of every genuine listing for the same query — an
    /// iPhone 15 "for 20 SAR" is not a real phone. With enough offers to
    /// judge a genuine cluster, drop anything priced under 20% of the
    /// median; below that count there isn't enough signal to safely guess.
    /// </summary>
    private static List<StoreOffer> RemovePriceOutliers(List<StoreOffer> offers)
    {
        if (offers.Count < 3)
        {
            return offers;
        }

        var sorted = offers.Select(o => o.Price).OrderBy(p => p).ToList();
        var mid = sorted.Count / 2;
        var median = sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2m;
        var threshold = median * 0.2m;

        return offers.Where(o => o.Price >= threshold).ToList();
    }

    private static bool IsNoon(string? source) =>
        !string.IsNullOrWhiteSpace(source) && source.Contains("noon", StringComparison.OrdinalIgnoreCase);

    private static readonly string[] StopWords = ["a", "an", "the", "for", "of", "with", "and", "in", "on", "to", "by"];
    private static readonly char[] TokenSeparators = [' ', '-', '_', ',', '.', '/', '(', ')'];

    /// <summary>
    /// Google Shopping sometimes mixes in loosely-"related" items that share
    /// no real overlap with what was actually searched (a genuine complaint:
    /// results "completely unrelated" to the query). Requiring most of the
    /// query's meaningful words to appear in the title is a simple, honest
    /// precision filter — it only ever removes items, never invents data.
    /// </summary>
    private static bool IsRelevant(string query, string title)
    {
        var queryTokens = Tokenize(query);
        if (queryTokens.Count == 0)
        {
            return true;
        }

        var titleTokens = Tokenize(title);
        var matches = queryTokens.Count(qt => titleTokens.Any(tt => TokensMatch(qt, tt)));

        // A short "brand + model" query (the most common shape here) must
        // match on every word — a real observed failure: "Samsung A70" (2
        // tokens) let an unrelated "itel A70" phone through, because it only
        // needed 1-of-2 words (the shared model number) under a plain
        // majority rule. Longer queries keep majority-rounded-up slack,
        // since Google's own title wording varies more the more it's asked.
        var required = queryTokens.Count <= 2 ? queryTokens.Count : (queryTokens.Count + 1) / 2;
        return matches >= Math.Max(1, required);
    }

    /// <summary>
    /// Exact match, or — only for alphabetic words of at least 4 letters — a
    /// prefix match to tolerate simple plurals/variants ("console"/
    /// "consoles"). Deliberately excludes short/numeric tokens ("5", "15"),
    /// since substring containment there would wrongly match "5" against
    /// "55" or "2500".
    /// </summary>
    private static bool TokensMatch(string a, string b)
    {
        if (a == b)
        {
            return true;
        }

        if (a.Length < 4 || b.Length < 4 || !a.All(char.IsLetter) || !b.All(char.IsLetter))
        {
            return false;
        }

        return a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal);
    }

    // A real, observed failure mode: searching a phone model ("Samsung A70")
    // returned repair parts, a repair *service*, and a case/cover — all
    // priced far below the device — as the "best deal", because small
    // resellers list parts/services/accessories under the exact model name
    // Google then matches on. None of these are the product itself, so we
    // filter on real, observed signals: accessory nouns, repair/spare-part
    // language, and their Arabic equivalents (Noon and other regional
    // listings return Arabic titles even when hl=en is requested).
    private static readonly string[] AccessoryKeywords =
    [
        "case", "cover", "skin", "screen protector", "tempered glass", "pouch",
        "holder", "stand", "strap", "charger", "cable", "adapter", "sticker",
        "protector", "bumper", "sleeve", "mount", "stylus", "earphone", "earbud",
        "headphone", "power bank", "memory card", "sim card",
        // Repair parts/services — a different failure mode than accessories,
        // but the same root cause (matched on model name, not the product).
        "housing", "back panel", "rear panel", "rear housing", "back cover",
        "battery", "lcd", "display unit", "digitizer", "touch screen", "flex cable",
        "replacement screen", "screen assembly", "screen replacement",
        "motherboard", "mainboard", "logic board", "charging port", "dock connector",
        "camera lens", "repair service", "repair kit", "inspection service",
        "spare part", "spare parts", "sim tray", "rear glass", "back glass",
        // Apparel/merch bundled in by resellers (e.g. a console search
        // surfacing a "T-Shirt & Controller" bundle) — unambiguous, since
        // no genuine device listing mentions clothing. Deliberately not
        // excluding bare "controller": a real "Console with Wireless
        // Controller" bundle listing would be wrongly caught by that, since
        // controllers are a genuine part of many authentic console bundles.
        "shirt", "hoodie", "keychain", "poster",
        // Arabic equivalents.
        "غطاء", "واقي", "واق", "قطعة غيار", "قطع غيار", "صيانة", "اصلاح", "إصلاح", "كفر", "شاحن",
    ];

    /// <summary>
    /// True when the title is clearly an accessory FOR the searched product
    /// rather than the product itself — unless the query is itself looking
    /// for that accessory (e.g. a genuine "iphone 15 case" search).
    /// </summary>
    private static bool IsAccessoryMismatch(string query, string title)
    {
        var queryLower = query.ToLowerInvariant();

        // If the query itself signals accessory intent, trust it wholesale —
        // a title using a different accessory synonym ("case" vs. "cover")
        // than the query shouldn't still get excluded.
        if (AccessoryKeywords.Any(keyword => queryLower.Contains(keyword, StringComparison.Ordinal)))
        {
            return false;
        }

        var titleLower = title.ToLowerInvariant();
        return AccessoryKeywords.Any(keyword => titleLower.Contains(keyword, StringComparison.Ordinal));
    }

    private static List<string> Tokenize(string text) =>
        text
            .ToLowerInvariant()
            .Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !StopWords.Contains(w))
            .Distinct()
            .ToList();

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
        [property: JsonPropertyName("link")] string? Link,
        [property: JsonPropertyName("rating")] double? Rating,
        [property: JsonPropertyName("ratingCount")] int? RatingCount);
}
