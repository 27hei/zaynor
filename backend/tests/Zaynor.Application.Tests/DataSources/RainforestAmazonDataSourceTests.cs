using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the Rainforest live source maps real Amazon.sa API results into
/// StoreOffers correctly and stays dormant with no key. The HTTP layer is
/// stubbed so this runs offline; the live connection itself is verified
/// separately once a real key is configured.
/// </summary>
public class RainforestAmazonDataSourceTests
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
    public async Task WithApiKey_MapsRealResults_ToStoreOffers()
    {
        const string json = """
        {
          "search_results": [
            {
              "title": "Apple iPhone 15 (128 GB) - Black",
              "link": "https://www.amazon.sa/dp/B0CHXGL9PN",
              "image": "https://m.media-amazon.com/images/I/61bCKBrMVNL.jpg",
              "price": { "value": 2561.00, "currency": "SAR" }
            },
            {
              "title": "Sponsored row with no price",
              "link": "https://www.amazon.sa/dp/XXXX",
              "price": null
            }
          ]
        }
        """;
        var handler = new StubHandler(json);
        var source = new RainforestAmazonDataSource(
            new StubFactory(handler),
            Config(("DataSources:Rainforest:ApiKey", "test-key")),
            NullLogger<RainforestAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        // The price-less sponsored row is dropped; only the real offer survives.
        Assert.Single(offers);
        var offer = offers[0];
        Assert.Equal("Amazon.sa", offer.StoreName);
        Assert.Equal("Apple iPhone 15 (128 GB) - Black", offer.ProductTitle);
        Assert.Equal(2561.00m, offer.Price);
        Assert.Equal("SAR", offer.Currency);
        Assert.Equal("https://www.amazon.sa/dp/B0CHXGL9PN", offer.ProductUrl);
        Assert.Equal("https://m.media-amazon.com/images/I/61bCKBrMVNL.jpg", offer.ImageUrl);

        // The request targets the Saudi marketplace with the search term.
        Assert.Contains("amazon_domain=amazon.sa", handler.RequestedUrl);
        Assert.Contains("search_term=iphone", handler.RequestedUrl);
    }

    [Fact]
    public async Task WithoutApiKey_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler("{}");
        var source = new RainforestAmazonDataSource(
            new StubFactory(handler),
            Config(), // no key configured
            NullLogger<RainforestAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        Assert.False(source.IsEnabled);
        Assert.Empty(offers);
        Assert.Null(handler.RequestedUrl); // never even called the API
    }
}
