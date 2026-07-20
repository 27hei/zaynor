using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the AliExpress live source signs requests per the platform scheme,
/// maps real affiliate-API results into StoreOffers, and stays dormant with no
/// credentials. HTTP is stubbed so this runs offline; the live handshake is
/// verified separately once real app credentials are configured.
/// </summary>
public class AliExpressProductDataSourceTests
{
    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        public string? RequestedUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUrl = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v =>
                new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void BuildSignature_MatchesPlatformScheme_SortedConcatHmacUpperHex()
    {
        // Independently reproduce the documented algorithm and assert the
        // source computes the identical value: sort by key, concat key+value,
        // HMAC-SHA256 with the secret, hex uppercase.
        const string secret = "test-secret";
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["app_key"] = "12345",
            ["method"] = "aliexpress.affiliate.product.query",
            ["keywords"] = "iphone",
        };

        var concatenated = "app_key12345keywordsiphonemethodaliexpress.affiliate.product.query";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenated)));

        var actual = AliExpressProductDataSource.BuildSignature(secret, parameters);

        Assert.Equal(expected, actual);
        Assert.Equal(64, actual.Length);           // SHA-256 → 32 bytes → 64 hex chars
        Assert.Equal(actual.ToUpperInvariant(), actual); // uppercase
    }

    [Fact]
    public async Task WithCredentials_MapsRealProducts_ToStoreOffers()
    {
        const string json = """
        {
          "aliexpress_affiliate_product_query_response": {
            "resp_result": {
              "resp_code": 200,
              "result": {
                "products": {
                  "product": [
                    {
                      "product_title": "Wireless Earbuds Pro",
                      "product_main_image_url": "https://ae01.alicdn.com/kf/x.jpg",
                      "target_sale_price": "89.50",
                      "target_sale_price_currency": "SAR",
                      "promotion_link": "https://s.click.aliexpress.com/e/_abc123",
                      "product_detail_url": "https://www.aliexpress.com/item/100.html"
                    },
                    {
                      "product_title": "No price item",
                      "product_detail_url": "https://www.aliexpress.com/item/200.html"
                    }
                  ]
                }
              }
            }
          }
        }
        """;
        var handler = new StubHandler(json);
        var source = new AliExpressProductDataSource(
            new StubFactory(handler),
            Config(
                ("DataSources:AliExpress:AppKey", "12345"),
                ("DataSources:AliExpress:AppSecret", "test-secret"),
                ("DataSources:AliExpress:TrackingId", "zaynor")),
            NullLogger<AliExpressProductDataSource>.Instance);

        var offers = await source.SearchAsync("earbuds");

        Assert.Single(offers); // the price-less row is dropped
        var offer = offers[0];
        Assert.Equal("AliExpress", offer.StoreName);
        Assert.Equal("Wireless Earbuds Pro", offer.ProductTitle);
        Assert.Equal(89.50m, offer.Price);
        Assert.Equal("SAR", offer.Currency);
        // The affiliate promotion link is preferred (it carries the commission).
        Assert.Equal("https://s.click.aliexpress.com/e/_abc123", offer.ProductUrl);
        Assert.Equal("https://ae01.alicdn.com/kf/x.jpg", offer.ImageUrl);

        // Request targets the Saudi market in SAR and is signed.
        Assert.Contains("target_currency=SAR", handler.RequestedUrl);
        Assert.Contains("ship_to_country=SA", handler.RequestedUrl);
        Assert.Contains("sign=", handler.RequestedUrl);
    }

    [Fact]
    public async Task WithoutCredentials_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler("{}");
        var source = new AliExpressProductDataSource(
            new StubFactory(handler),
            Config(),
            NullLogger<AliExpressProductDataSource>.Instance);

        var offers = await source.SearchAsync("earbuds");

        Assert.False(source.IsEnabled);
        Assert.Empty(offers);
        Assert.Null(handler.RequestedUrl); // never called the API
    }
}
