using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.DataSources;

/// <summary>
/// A REAL, live data source: millions of AliExpress products (real prices,
/// images, and affiliate-monetized links) via the AliExpress Open Platform
/// affiliate API (aliexpress.affiliate.product.query on the api-sg.aliexpress.
/// com/sync gateway). Free to use once an affiliate app is approved. The
/// promotion_link the API returns is already an affiliate link, so clicks are
/// monetized without /api/out having to add anything.
///
/// Config-only activation: dormant until DataSources:AliExpress:AppKey and
/// :AppSecret are set (env: DataSources__AliExpress__AppKey /
/// DataSources__AliExpress__AppSecret / __TrackingId). Prices are requested in
/// SAR, shipping to SA. Signing follows the platform's documented scheme:
/// sort every parameter by key, concatenate key+value, HMAC-SHA256 with the
/// app secret, hex uppercase.
/// </summary>
public sealed class AliExpressProductDataSource : IProductDataSource
{
    private const string Gateway = "https://api-sg.aliexpress.com/sync";
    private const string Method = "aliexpress.affiliate.product.query";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AliExpressProductDataSource> _logger;
    private readonly string? _appKey;
    private readonly string? _appSecret;
    private readonly string? _trackingId;
    private readonly int _maxResults;

    public AliExpressProductDataSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AliExpressProductDataSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appKey = configuration["DataSources:AliExpress:AppKey"];
        _appSecret = configuration["DataSources:AliExpress:AppSecret"];
        _trackingId = configuration["DataSources:AliExpress:TrackingId"];
        // A store can now show more than one genuinely different listing per
        // search — but not unbounded (this is a rate-limited API), so a
        // shared, config-driven cap across all single-call sources.
        _maxResults = int.TryParse(configuration["DataSources:MaxListingsPerSource"], out var max) ? max : 10;
    }

    public string SourceName => "AliExpress";

    /// <summary>Active only once app credentials are configured; otherwise dormant.</summary>
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(_appKey) && !string.IsNullOrWhiteSpace(_appSecret);

    /// <summary>Rate-limited API — queried on every search, alongside the curated catalog and other live feeds.</summary>
    public bool IsExpensiveLive => true;

    /// <summary>A direct store scraper, same tier as the Amazon sources but less scrutinized this session.</summary>
    public double Confidence => 0.8;

    public async Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Array.Empty<StoreOffer>();
        }

        try
        {
            // Sorted so the signature is computed over a stable ordering.
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["app_key"] = _appKey!,
                ["sign_method"] = "sha256",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                ["method"] = Method,
                ["keywords"] = query,
                ["target_currency"] = "SAR",
                ["target_language"] = "EN",
                ["ship_to_country"] = "SA",
                ["page_no"] = "1",
                ["page_size"] = _maxResults.ToString(CultureInfo.InvariantCulture),
            };
            if (!string.IsNullOrWhiteSpace(_trackingId))
            {
                parameters["tracking_id"] = _trackingId!;
            }

            parameters["sign"] = BuildSignature(_appSecret!, parameters);

            var queryString = string.Join('&', parameters.Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

            var client = _httpClientFactory.CreateClient(nameof(AliExpressProductDataSource));
            var envelope = await client.GetFromJsonAsync<AliExpressEnvelope>(
                $"{Gateway}?{queryString}", cancellationToken);

            var products = envelope?.Response?.RespResult?.Result?.Products?.Product ?? [];

            var offers = new List<StoreOffer>();
            foreach (var product in products)
            {
                if (string.IsNullOrWhiteSpace(product.Title)
                    || !decimal.TryParse(product.SalePrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                    || price <= 0
                    // Now that more than one listing per query can survive,
                    // apply the same precision filters GoogleShoppingDataSource
                    // needed once it stopped taking just a single top result.
                    || !ListingRelevanceFilter.IsRelevant(query, product.Title)
                    || ListingRelevanceFilter.IsAccessoryMismatch(query, query, product.Title))
                {
                    continue;
                }

                var url = !string.IsNullOrWhiteSpace(product.PromotionLink)
                    ? product.PromotionLink!
                    : product.DetailUrl;
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                offers.Add(new StoreOffer
                {
                    StoreName = "AliExpress",
                    ProductTitle = product.Title!,
                    Price = price,
                    Currency = string.IsNullOrWhiteSpace(product.SalePriceCurrency) ? "SAR" : product.SalePriceCurrency!,
                    ProductUrl = url!,
                    InStock = true,
                    ImageUrl = product.ImageUrl,
                    FreeShipping = false,
                    DeliveryDays = null,
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
            _logger.LogWarning(ex, "AliExpress source failed for query {Query}; skipping it", query);
            return Array.Empty<StoreOffer>();
        }
    }

    /// <summary>
    /// AliExpress Open Platform signature: sort params by key, concatenate as
    /// key+value with no separators, HMAC-SHA256 with the app secret, hex
    /// uppercase. (The gateway path is empty for /sync, so nothing is prepended.)
    /// </summary>
    public static string BuildSignature(string appSecret, IEnumerable<KeyValuePair<string, string>> sortedParameters)
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in sortedParameters)
        {
            builder.Append(key).Append(value);
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash); // uppercase hex
    }

    private sealed record AliExpressEnvelope(
        [property: JsonPropertyName("aliexpress_affiliate_product_query_response")] AliExpressResponse? Response);

    private sealed record AliExpressResponse(
        [property: JsonPropertyName("resp_result")] AliExpressRespResult? RespResult);

    private sealed record AliExpressRespResult(
        [property: JsonPropertyName("result")] AliExpressResult? Result);

    private sealed record AliExpressResult(
        [property: JsonPropertyName("products")] AliExpressProducts? Products);

    private sealed record AliExpressProducts(
        [property: JsonPropertyName("product")] List<AliExpressProduct>? Product);

    private sealed record AliExpressProduct(
        [property: JsonPropertyName("product_title")] string? Title,
        [property: JsonPropertyName("product_main_image_url")] string? ImageUrl,
        [property: JsonPropertyName("target_sale_price")] string? SalePrice,
        [property: JsonPropertyName("target_sale_price_currency")] string? SalePriceCurrency,
        [property: JsonPropertyName("promotion_link")] string? PromotionLink,
        [property: JsonPropertyName("product_detail_url")] string? DetailUrl);
}
