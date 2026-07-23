using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the DataForSEO live source maps real Amazon.sa API results into
/// StoreOffers correctly and stays dormant with no credentials. The HTTP
/// layer is stubbed so this runs offline; the live connection itself is
/// verified separately once real credentials are configured.
/// </summary>
public class DataForSeoAmazonDataSourceTests
{
    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
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
    public async Task WithCredentials_MapsRealResults_ToStoreOffers()
    {
        const string json = """
        {
          "tasks": [
            {
              "result": [
                {
                  "items": [
                    {
                      "type": "amazon_serp",
                      "title": "Apple iPhone 15 (128 GB) - Black",
                      "url": "https://www.amazon.sa/dp/B0CHXGL9PN",
                      "image_url": "https://m.media-amazon.com/images/I/61bCKBrMVNL.jpg",
                      "price_from": 2561.00,
                      "currency": "SAR"
                    },
                    {
                      "type": "amazon_paid",
                      "title": "Sponsored placement",
                      "url": "https://www.amazon.sa/dp/XXXX",
                      "price_from": 999.00,
                      "currency": "SAR"
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;
        var handler = new StubHandler(json);
        var source = new DataForSeoAmazonDataSource(
            new StubFactory(handler),
            Config(
                ("DataSources:DataForSeo:Login", "test@example.com"),
                ("DataSources:DataForSeo:Password", "test-password")),
            NullLogger<DataForSeoAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        // The sponsored ("amazon_paid") row is dropped; only the organic offer survives.
        Assert.Single(offers);
        var offer = offers[0];
        Assert.Equal("Amazon.sa", offer.StoreName);
        Assert.Equal("Apple iPhone 15 (128 GB) - Black", offer.ProductTitle);
        Assert.Equal(2561.00m, offer.Price);
        Assert.Equal("SAR", offer.Currency);
        Assert.Equal("https://www.amazon.sa/dp/B0CHXGL9PN", offer.ProductUrl);
        Assert.Equal("https://m.media-amazon.com/images/I/61bCKBrMVNL.jpg", offer.ImageUrl);

        // Basic auth carries the configured login:password, and the body
        // targets the Saudi marketplace with the search term.
        Assert.NotNull(handler.Request);
        Assert.Equal("Basic", handler.Request!.Headers.Authorization?.Scheme);
        var expectedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes("test@example.com:test-password"));
        Assert.Equal(expectedAuth, handler.Request!.Headers.Authorization?.Parameter);
        Assert.Contains("iphone 15", handler.RequestBody);
        Assert.Contains("Saudi Arabia", handler.RequestBody);
    }

    [Fact]
    public async Task WithoutCredentials_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler("{}");
        var source = new DataForSeoAmazonDataSource(
            new StubFactory(handler),
            Config(), // no credentials configured
            NullLogger<DataForSeoAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        Assert.False(source.IsEnabled);
        Assert.Empty(offers);
        Assert.Null(handler.Request); // never even called the API
    }
}
