using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.DataSources;

/// <summary>
/// A REAL, live data source: prices/images across every merchant Google
/// Shopping tracks for a query, via HasData's Google Shopping + Immersive
/// Product scraper endpoints — a second, independent path to the same real
/// multi-merchant coverage GoogleShoppingDataSource (SerpApi) provides,
/// added because SerpApi's own account hit a persistent 429 rate limit that
/// was never resolved, leaving searches with real Saudi merchant coverage
/// (Noon, Jarir, stc, ...) missing for most of this session. Verified
/// directly against the real API with a real trial key: a Saudi Arabia
/// query for "samsung galaxy watch 7" (gl=sa) returned real, distinct
/// merchants — stc, Amazon.sa, Microless.com, eBay, and more — each with
/// its own real price and direct product link, confirming both locale
/// support and the two-step shopping-search + immersive-product-expansion
/// pattern this class mirrors from GoogleShoppingDataSource.
///
/// Uses hl=en (not hl=ar) even for the Saudi Arabia marketplace — same
/// deliberate choice GoogleShoppingDataSource and DataForSeoAmazonDataSource
/// already made for their own Saudi queries. Verified directly: hl=ar
/// returns merchant titles in Arabic script, which ListingRelevanceFilter
/// (built around Latin-script/transliterated tokens) can't match against an
/// English query, silently dropping real, relevant offers; hl=en keeps most
/// listing titles and ALL prices in ASCII while still returning the same
/// real Saudi merchants and SAR pricing.
///
/// HasData's stores[].price/total fields can still be formatted Arabic
/// strings with Arabic-Indic digits and RTL marks in some listings even
/// under hl=en (e.g. "‏٧٧٢٫٣٤ ر.س.‏" = 772.34 SAR), rather than always a
/// clean "SAR 699.00" — ParseArabicPrice below handles both shapes.
///
/// Config-only activation: dormant until DataSources:HasData:ApiKey is set
/// (env: DataSources__HasData__ApiKey).
/// </summary>
public sealed class HasDataShoppingDataSource : IProductDataSource
{
    private const string ShoppingEndpoint = "https://api.hasdata.com/scrape/google/shopping";

    // Each expansion is its own billed call, so only the top N candidate
    // products get resolved to real per-merchant links — same reasoning and
    // budget as GoogleShoppingDataSource.
    private const int MaxProductsToExpand = 4;
    private const int MaxResults = 30;
    private const int MaxSpecifications = 12;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HasDataShoppingDataSource> _logger;
    private readonly string? _apiKey;
    private readonly string _linkSigningKey;

    public HasDataShoppingDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HasDataShoppingDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["DataSources:HasData:ApiKey"];
        // Reuses the app's JWT signing key for outbound-link signatures
        // (see StoreOffer.Signature/OutboundLinkSigner), same as
        // GoogleShoppingDataSource — a real secret already provisioned for
        // this app, no need for a second one.
        _linkSigningKey = configuration["Jwt:Key"] ?? string.Empty;
    }

    public string SourceName => "HasDataShopping";

    /// <summary>Active only once an API key is configured; otherwise fully dormant.</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>Paid-per-request API — queried on every search, alongside the curated catalog and other live feeds.</summary>
    public bool IsExpensiveLive => true;

    /// <summary>Resolves real per-merchant links via a verified two-step API, but still a scrape of an aggregator — same tier as GoogleShoppingDataSource.</summary>
    public double Confidence => 0.9;

    public async Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Array.Empty<StoreOffer>();
        }

        try
        {
            var effectiveQuery = ArabicBrandNormalizer.Normalize(query);
            var client = _httpClientFactory.CreateClient(nameof(HasDataShoppingDataSource));
            var products = await FetchShoppingResultsAsync(client, effectiveQuery, cancellationToken);

            var toExpand = products
                .Where(p => !string.IsNullOrWhiteSpace(p.HasdataLink))
                .Take(MaxProductsToExpand)
                .ToList();

            // Independent calls — run together so total latency stays close
            // to one round trip instead of stacking sequentially.
            var expansions = await Task.WhenAll(toExpand.Select(p =>
                FetchProductResultsAsync(client, p.HasdataLink!, cancellationToken)));

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
                        || ParseArabicPrice(store.Price) is not { } price || price <= 0
                        // Merchants return titles in whichever script they
                        // list in — judged relevant if it matches in EITHER
                        // its original or brand-normalized form, same as
                        // GoogleShoppingDataSource.
                        || (!ListingRelevanceFilter.IsRelevant(query, listingTitle) && !ListingRelevanceFilter.IsRelevant(effectiveQuery, listingTitle))
                        || ListingRelevanceFilter.IsAccessoryMismatch(query, effectiveQuery, listingTitle))
                    {
                        continue;
                    }

                    var isNoon = !string.IsNullOrWhiteSpace(store.Name) && store.Name.Contains("noon", StringComparison.OrdinalIgnoreCase);
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
                            // A missing/"free" shipping field means nothing
                            // extra was added on top of price; a real
                            // shipping cost is surfaced via "shipping" (e.g.
                            // "+ ٢٩٫٩٩ ر.س.") — never fabricated either way.
                            FreeShipping = string.IsNullOrWhiteSpace(store.Shipping) || store.Shipping.Contains("مجانًا"),
                            DeliveryDays = null,
                            Rating = store.Rating is { } r ? (decimal)r : productResults?.Rating is { } pr ? (decimal)pr : null,
                            RatingCount = store.Reviews ?? productResults?.Reviews,
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
            _logger.LogWarning(ex, "HasData Shopping source failed for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
    }

    private async Task<List<HasDataShoppingResult>> FetchShoppingResultsAsync(
        HttpClient client, string query, CancellationToken cancellationToken)
    {
        var url = $"{ShoppingEndpoint}?q={Uri.EscapeDataString(query)}"
            + $"&location={Uri.EscapeDataString("Saudi Arabia")}&gl=sa&hl=en&deviceType=desktop";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-api-key", _apiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<HasDataShoppingEnvelope>(cancellationToken: cancellationToken);
        return envelope?.ShoppingResults ?? [];
    }

    private async Task<HasDataProductResults?> FetchProductResultsAsync(
        HttpClient client, string hasdataLink, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, hasdataLink);
        request.Headers.Add("x-api-key", _apiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<HasDataImmersiveEnvelope>(cancellationToken: cancellationToken);
        return envelope?.ProductResults;
    }

    /// <summary>
    /// Builds detail fields from data already fetched (never fabricated,
    /// never an extra API call) — null when nothing usable is present so no
    /// offer carries an all-empty wrapper object. Brand/specs are
    /// product-level (shared across every store of this product);
    /// StoreHighlights is this specific store's own fulfillment info. No
    /// per-product image array here (HasData's immersive response doesn't
    /// carry one) — ImageUrl already comes from the shopping-search step.
    /// </summary>
    private static ProductDetails? BuildProductDetails(HasDataProductResults? productResults, HasDataStore store)
    {
        if (productResults is null)
        {
            return null;
        }

        var brand = string.IsNullOrWhiteSpace(productResults.Brand) ? null : productResults.Brand;
        var specs = productResults.AboutTheProduct?.Features is { Count: > 0 } features
            ? features
                .Where(f => !string.IsNullOrWhiteSpace(f.Title) && !string.IsNullOrWhiteSpace(f.Value))
                .Take(MaxSpecifications)
                .Select(f => $"{f.Title}: {f.Value}")
                .ToList()
            : null;
        var highlights = store.DetailsAndOffers is { Count: > 0 } h ? h : null;

        if (brand is null && specs is not { Count: > 0 } && highlights is null)
        {
            return null;
        }

        return new ProductDetails
        {
            Images = null,
            Brand = brand,
            Description = null,
            Specifications = specs is { Count: > 0 } ? specs : null,
            StoreHighlights = highlights,
        };
    }

    /// <summary>
    /// HasData's price/total fields are formatted RTL Arabic strings with
    /// Arabic-Indic digits and a currency suffix (e.g. "‏٧٧٢٫٣٤ ر.س.‏" =
    /// 772.34 SAR) rather than a parsed number. Converts Arabic-Indic digits
    /// (٠-٩) and the Arabic decimal separator (٫) to their ASCII
    /// equivalents, drops everything else (RTL marks, currency text,
    /// spaces), then parses.
    /// </summary>
    private static decimal? ParseArabicPrice(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var builder = new StringBuilder();
        const char arabicDigitZero = (char)0x0660; // Arabic-Indic digit zero
        const char arabicDigitNine = (char)0x0669; // Arabic-Indic digit nine
        const char arabicDecimalSeparator = (char)0x066B; // Arabic decimal separator

        foreach (var c in raw)
        {
            if (c >= arabicDigitZero && c <= arabicDigitNine)
            {
                builder.Append((char)('0' + (c - arabicDigitZero)));
            }
            else if (c == arabicDecimalSeparator)
            {
                builder.Append('.');
            }
            else if (char.IsDigit(c) || c == '.')
            {
                builder.Append(c);
            }
            // Real observed shape: "٥٩٩٫٠٠ ر.س." — the currency abbreviation
            // ("ر.س.") that follows the number has its own periods, which
            // would otherwise get swept up by the "|| c == '.'" branch above
            // and break decimal.TryParse with multiple decimal points.
            // Stopping at the first whitespace once digits have started
            // cleanly separates the number from the currency text.
            else if (builder.Length > 0 && char.IsWhiteSpace(c))
            {
                break;
            }
        }

        return decimal.TryParse(builder.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private sealed record HasDataShoppingEnvelope(
        [property: JsonPropertyName("shoppingResults")] List<HasDataShoppingResult>? ShoppingResults);

    private sealed record HasDataShoppingResult(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("thumbnail")] string? Thumbnail,
        [property: JsonPropertyName("hasdataLink")] string? HasdataLink);

    private sealed record HasDataImmersiveEnvelope(
        [property: JsonPropertyName("productResults")] HasDataProductResults? ProductResults);

    private sealed record HasDataProductResults(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("brand")] string? Brand,
        [property: JsonPropertyName("rating")] double? Rating,
        [property: JsonPropertyName("reviews")] int? Reviews,
        [property: JsonPropertyName("stores")] List<HasDataStore>? Stores,
        [property: JsonPropertyName("aboutTheProduct")] HasDataAboutTheProduct? AboutTheProduct);

    private sealed record HasDataAboutTheProduct(
        [property: JsonPropertyName("features")] List<HasDataFeature>? Features);

    private sealed record HasDataFeature(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("value")] string? Value);

    private sealed record HasDataStore(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("link")] string? Link,
        [property: JsonPropertyName("price")] string? Price,
        [property: JsonPropertyName("shipping")] string? Shipping,
        [property: JsonPropertyName("total")] string? Total,
        [property: JsonPropertyName("rating")] double? Rating,
        [property: JsonPropertyName("reviews")] int? Reviews,
        [property: JsonPropertyName("detailsAndOffers")] List<string>? DetailsAndOffers);
}
