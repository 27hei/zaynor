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

    /// <summary>Resolves real per-merchant links via a verified two-step API, but still a scrape of an aggregator.</summary>
    public double Confidence => 0.9;

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
                FetchProductResultsAsync(client, p.ImmersiveProductPageToken!, cancellationToken)));

            var bestPerMerchant = new Dictionary<string, StoreOffer>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < toExpand.Count; i++)
            {
                var product = toExpand[i];
                var productResults = expansions[i];
                foreach (var store in productResults?.Stores ?? [])
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
                        || (!ListingRelevanceFilter.IsRelevant(query, listingTitle) && !ListingRelevanceFilter.IsRelevant(effectiveQuery, listingTitle))
                        || ListingRelevanceFilter.IsAccessoryMismatch(query, effectiveQuery, listingTitle))
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
                            ProductDetails = BuildProductDetails(productResults, store),
                        };
                    }
                }
            }

            return ListingRelevanceFilter.RemovePriceOutliers(bestPerMerchant.Values.ToList()).Take(MaxResults).ToList();
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

    private async Task<SerpApiProductResults?> FetchProductResultsAsync(
        HttpClient client, string pageToken, CancellationToken cancellationToken)
    {
        var url = $"{Endpoint}?engine=google_immersive_product&page_token={Uri.EscapeDataString(pageToken)}&more_stores=true&api_key={Uri.EscapeDataString(_apiKey!)}";
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<SerpApiImmersiveEnvelope>(cancellationToken: cancellationToken);
        return envelope?.ProductResults;
    }

    // A cap, not a completeness guarantee — some products carry dozens of
    // spec features (verified live: a real iPhone 15 response had 40+), far
    // more than a detail page needs to be useful.
    private const int MaxSpecifications = 12;

    /// <summary>
    /// Builds detail fields from data already fetched (never fabricated,
    /// never an extra API call) — null when nothing usable is present so no
    /// offer carries an all-empty wrapper object. Images/brand/description/
    /// specs are product-level (shared across every store of this product);
    /// StoreHighlights is this specific store's own fulfillment info.
    /// </summary>
    private static ProductDetails? BuildProductDetails(SerpApiProductResults? productResults, SerpApiStore store)
    {
        if (productResults is null)
        {
            return null;
        }

        var images = productResults.Thumbnails is { Count: > 0 } t ? t : null;
        var brand = string.IsNullOrWhiteSpace(productResults.Brand) ? null : productResults.Brand;
        var description = string.IsNullOrWhiteSpace(productResults.AboutTheProduct?.Description)
            ? null
            : productResults.AboutTheProduct.Description;
        var specs = productResults.AboutTheProduct?.Features is { Count: > 0 } features
            ? features
                .Where(f => !string.IsNullOrWhiteSpace(f.Title) && !string.IsNullOrWhiteSpace(f.Value))
                .Take(MaxSpecifications)
                .Select(f => $"{f.Title}: {f.Value}")
                .ToList()
            : null;
        var highlights = store.DetailsAndOffers is { Count: > 0 } h ? h : null;

        if (images is null && brand is null && description is null && specs is not { Count: > 0 } && highlights is null)
        {
            return null;
        }

        return new ProductDetails
        {
            Images = images,
            Brand = brand,
            Description = description,
            Specifications = specs is { Count: > 0 } ? specs : null,
            StoreHighlights = highlights,
        };
    }

    /// <summary>SerpApi provides a parsed <c>extracted_price</c> number; the raw "SAR X.XX" string is only a fallback.</summary>
    private static decimal? ResolvePrice(double? extractedPrice, string? priceRaw) =>
        extractedPrice is { } p ? (decimal)p : ParsePrice(priceRaw);

    private static bool IsNoon(string? source) =>
        !string.IsNullOrWhiteSpace(source) && source.Contains("noon", StringComparison.OrdinalIgnoreCase);

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
        [property: JsonPropertyName("stores")] List<SerpApiStore>? Stores,
        [property: JsonPropertyName("thumbnails")] List<string>? Thumbnails,
        [property: JsonPropertyName("brand")] string? Brand,
        [property: JsonPropertyName("about_the_product")] SerpApiAboutTheProduct? AboutTheProduct);

    /// <summary>Google's own "about this product" reference card — verified live: its link/title often points at a DIFFERENT listing (e.g. the manufacturer's own page) than any store in our results, so only the general description/features are usable here, never its link.</summary>
    private sealed record SerpApiAboutTheProduct(
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("features")] List<SerpApiFeature>? Features);

    private sealed record SerpApiFeature(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("value")] string? Value);

    private sealed record SerpApiStore(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("link")] string? Link,
        [property: JsonPropertyName("price")] string? Price,
        [property: JsonPropertyName("extracted_price")] double? ExtractedPrice,
        [property: JsonPropertyName("rating")] double? Rating,
        [property: JsonPropertyName("reviews")] int? Reviews,
        [property: JsonPropertyName("details_and_offers")] List<string>? DetailsAndOffers);
}
