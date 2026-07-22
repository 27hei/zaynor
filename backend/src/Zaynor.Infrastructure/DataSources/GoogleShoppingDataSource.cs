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
/// Shopping tracks for a query, via SerpApi — not just Noon.
///
/// Real reported bug this replaces an earlier attempt at: a flat Google
/// Shopping search result only ever carries Google's own "prds=" product-
/// panel link for each listing, which is a client-side overlay tied to the
/// search session that generated it — opened fresh (a new tab, a different
/// device, after time passes) it reliably shows "no details available for
/// this product" instead of the offer, for ANY merchant, and there is no
/// reliable way to guess a given merchant's own site-search URL pattern
/// (verified by hand: several real store domains simply don't follow a
/// predictable pattern). SerpApi's separate Google Immersive Product API —
/// the same panel a real user expands by clicking a product in Google
/// Shopping — genuinely returns each seller's own direct product-page URL
/// (verified: Jarir, Amazon, eXtra, desertcart and multiple small resellers
/// all came back with real, working links). This source calls Shopping
/// first for the candidate products, then expands a capped number of them
/// through Immersive Product to resolve real per-merchant links — the two
/// calls run per query, so store coverage is real but bounded (spec:
/// founder's call, chosen over paying for many more expansions).
///
/// Config-only activation: dormant until DataSources:SerpApi:ApiKey is set
/// (env: DataSources__SerpApi__ApiKey). Separate from DataSources:Serper,
/// which SerperLensQueryResolver still uses for reverse-image search.
/// </summary>
public sealed class GoogleShoppingDataSource : IProductDataSource
{
    private const string Endpoint = "https://serpapi.com/search.json";

    // Each expansion is its own billed SerpApi call, so only the top N
    // candidate products get resolved to real per-merchant links. One
    // expansion alone regularly returns up to 13 real stores, so this
    // still gives broad coverage without letting cost scale with the
    // dozens of raw listings Google Shopping can return for a query.
    private const int MaxProductsToExpand = 4;
    private const int MaxResults = 30;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleShoppingDataSource> _logger;
    private readonly string? _apiKey;
    private readonly string _linkSigningKey;

    public GoogleShoppingDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GoogleShoppingDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["DataSources:SerpApi:ApiKey"];
        // Reuses the app's JWT signing key for outbound-link signatures
        // (see StoreOffer.Signature/OutboundLinkSigner) — a real secret
        // already provisioned for this app, no need for a second one.
        _linkSigningKey = configuration["Jwt:Key"] ?? string.Empty;
    }

    public string SourceName => "GoogleShopping";

    /// <summary>Active only once a SerpApi key is configured; otherwise fully dormant.</summary>
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
            // A real observed failure: "سامسنج A70" (a common colloquial
            // Arabic spelling of Samsung, missing a "و") returned nothing at
            // all from Google Shopping, silently falling all the way back to
            // demo data — while the standard spelling "سامسونج" and the
            // English "Samsung" both return real results. Normalizing known
            // brand transliterations before searching (and before relevance
            // checks, so they're judged consistently) fixes this without
            // ever inventing data — it only helps Google understand what was
            // actually typed.
            var effectiveQuery = ArabicBrandNormalizer.Normalize(query);

            var client = _httpClientFactory.CreateClient(nameof(GoogleShoppingDataSource));
            var products = await FetchShoppingResultsAsync(client, effectiveQuery, cancellationToken);

            var toExpand = products
                .Where(p => !string.IsNullOrWhiteSpace(p.ImmersiveProductPageToken))
                .Take(MaxProductsToExpand)
                .ToList();

            // Independent calls — run together so total latency stays close
            // to one round trip instead of stacking sequentially.
            var expansions = await Task.WhenAll(toExpand.Select(p =>
                FetchStoresAsync(client, p.ImmersiveProductPageToken!, cancellationToken)));

            var bestPerMerchant = new Dictionary<string, StoreOffer>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < toExpand.Count; i++)
            {
                var product = toExpand[i];
                foreach (var store in expansions[i])
                {
                    var listingTitle = string.IsNullOrWhiteSpace(store.Title) ? product.Title : store.Title;

                    if (string.IsNullOrWhiteSpace(store.Name)
                        || string.IsNullOrWhiteSpace(store.Link)
                        || string.IsNullOrWhiteSpace(listingTitle)
                        || ResolvePrice(store.ExtractedPrice, store.Price) is not { } price || price <= 0
                        // Merchants return titles in whichever script they
                        // list in — a query is judged relevant/on-topic if
                        // it matches in EITHER its original or brand-
                        // normalized form (spec: real Arabic-titled Jarir
                        // listings must not be dropped just because
                        // "Samsung" doesn't literally appear in Arabic).
                        || (!IsRelevant(query, listingTitle) && !IsRelevant(effectiveQuery, listingTitle))
                        || IsAccessoryMismatch(query, effectiveQuery, listingTitle))
                    {
                        continue;
                    }

                    var isNoon = IsNoon(store.Name);
                    var storeName = isNoon ? "Noon" : store.Name!;

                    if (!bestPerMerchant.TryGetValue(storeName, out var existing) || price < existing.Price)
                    {
                        bestPerMerchant[storeName] = new StoreOffer
                        {
                            StoreName = storeName,
                            ProductTitle = listingTitle!,
                            Price = price,
                            Currency = "SAR",
                            ProductUrl = store.Link!,
                            InStock = true,
                            ImageUrl = product.Thumbnail,
                            FreeShipping = false,
                            DeliveryDays = null,
                            Rating = store.Rating is { } r ? (decimal)r : product.Rating is { } pr ? (decimal)pr : null,
                            RatingCount = store.Reviews ?? product.Reviews,
                            Signature = OutboundLinkSigner.Sign(store.Link!, _linkSigningKey),
                        };
                    }
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

    private async Task<List<SerpApiShoppingResult>> FetchShoppingResultsAsync(
        HttpClient client, string query, CancellationToken cancellationToken)
    {
        var url = $"{Endpoint}?engine=google_shopping&q={Uri.EscapeDataString(query)}&gl=sa&hl=en&api_key={Uri.EscapeDataString(_apiKey!)}";
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<SerpApiShoppingEnvelope>(cancellationToken: cancellationToken);
        return envelope?.ShoppingResults ?? [];
    }

    private async Task<List<SerpApiStore>> FetchStoresAsync(
        HttpClient client, string pageToken, CancellationToken cancellationToken)
    {
        var url = $"{Endpoint}?engine=google_immersive_product&page_token={Uri.EscapeDataString(pageToken)}&more_stores=true&api_key={Uri.EscapeDataString(_apiKey!)}";
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<SerpApiImmersiveEnvelope>(cancellationToken: cancellationToken);
        return envelope?.ProductResults?.Stores ?? [];
    }

    /// <summary>SerpApi provides a parsed <c>extracted_price</c> number; the raw "SAR X.XX" string is only a fallback.</summary>
    private static decimal? ResolvePrice(double? extractedPrice, string? priceRaw) =>
        extractedPrice is { } p ? (decimal)p : ParsePrice(priceRaw);

    /// <summary>
    /// A real observed gap in the keyword filters above: some listings
    /// (a mislabeled/deceptive accessory, or a case brand whose product name
    /// doesn't mention "case") carry no accessory keyword at all, yet price
    /// at a small fraction of every genuine listing for the same query — an
    /// iPhone 15 "for 20 SAR" is not a real phone. With enough offers to
    /// judge a genuine cluster, drop anything priced under 20% of the
    /// median; below that count there isn't enough signal to safely guess.
    ///
    /// Symmetric on the high side too — a real reported case: searching a
    /// personal-care product ("غسول...") once returned an item priced
    /// wildly above the rest of the genuine cluster (thousands of SAR
    /// versus tens on Amazon for the actual product), almost certainly a
    /// mismatched or wrong-currency-parsed listing that slipped past the
    /// relevance/keyword checks. 8x the median is generous enough to keep
    /// legitimate premium variants (e.g. a higher-storage phone model)
    /// while still catching genuine order-of-magnitude anomalies.
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
        var lowerBound = median * 0.2m;
        var upperBound = median * 8m;

        return offers.Where(o => o.Price >= lowerBound && o.Price <= upperBound).ToList();
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
        // Arabic equivalents — expanded after a real leak: searching
        // "سامسونج A70" (the standard-spelling Arabic query, which itself
        // needed the transliteration fix above to even reach Google Shopping
        // with results at all) returned six offers, and every single one was
        // a screen protector, SIM tray, back cover, battery, or replacement
        // screen — none of which "شاشة"/"بطارية"/"عدسة"/etc. below existed
        // to catch at the time.
        "غطاء", "غطا", "واقي", "واق", "قطعة غيار", "قطع غيار", "صيانة", "اصلاح", "إصلاح", "كفر", "شاحن",
        // "مدخل"/"فتحة" (slot/port) rather than "شريحة" (SIM) itself — a
        // genuine phone listing legitimately says "ثنائي الشريحة" (dual-SIM)
        // as a feature, so excluding on bare "شريحة" would wrongly catch
        // real phones too; the hardware-port word is what's actually unique
        // to the SIM-tray-as-a-spare-part listings.
        "شاشة", "بطارية", "عدسة", "مدخل", "فتحة", "لاصقة", "زجاج مقوى",
        "جراب", "حافظة", "كابل", "زجاج خلفي", "تيشرت", "قميص",
    ];

    /// <summary>
    /// True when the title is clearly an accessory FOR the searched product
    /// rather than the product itself — unless the query is itself looking
    /// for that accessory (e.g. a genuine "iphone 15 case" search). Checked
    /// against both the original and brand-normalized query text, since
    /// either could be the one carrying the accessory word the user typed.
    /// </summary>
    private static bool IsAccessoryMismatch(string query, string effectiveQuery, string title)
    {
        var q1 = query.ToLowerInvariant();
        var q2 = effectiveQuery.ToLowerInvariant();

        // If the query itself signals accessory intent, trust it wholesale —
        // a title using a different accessory synonym ("case" vs. "cover")
        // than the query shouldn't still get excluded.
        if (AccessoryKeywords.Any(keyword => q1.Contains(keyword, StringComparison.Ordinal) || q2.Contains(keyword, StringComparison.Ordinal)))
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

    /// <summary>Fallback for the rare case <c>extracted_price</c> is absent — parses a "SAR 2,699.00"-style string leniently.</summary>
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

    private sealed record SerpApiShoppingEnvelope(
        [property: JsonPropertyName("shopping_results")] List<SerpApiShoppingResult>? ShoppingResults);

    private sealed record SerpApiShoppingResult(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("thumbnail")] string? Thumbnail,
        [property: JsonPropertyName("rating")] double? Rating,
        [property: JsonPropertyName("reviews")] int? Reviews,
        [property: JsonPropertyName("immersive_product_page_token")] string? ImmersiveProductPageToken);

    private sealed record SerpApiImmersiveEnvelope(
        [property: JsonPropertyName("product_results")] SerpApiProductResults? ProductResults);

    private sealed record SerpApiProductResults(
        [property: JsonPropertyName("stores")] List<SerpApiStore>? Stores);

    private sealed record SerpApiStore(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("link")] string? Link,
        [property: JsonPropertyName("price")] string? Price,
        [property: JsonPropertyName("extracted_price")] double? ExtractedPrice,
        [property: JsonPropertyName("rating")] double? Rating,
        [property: JsonPropertyName("reviews")] int? Reviews);
}
