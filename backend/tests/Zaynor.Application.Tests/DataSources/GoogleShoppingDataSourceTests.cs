using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the open-scope Google Shopping (Serper) source maps every real
/// merchant into a StoreOffer (not just Noon), dedupes repeat listings from
/// the same merchant down to its cheapest, builds a taggable Noon
/// site-search URL specifically for Noon, and stays dormant with no key.
/// The HTTP layer is stubbed so this runs offline.
/// </summary>
public class GoogleShoppingDataSourceTests
{
    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
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
    public async Task WithApiKey_ReturnsEveryMerchant_DedupedAndMapped()
    {
        const string json = """
        {
          "shopping": [
            {
              "title": "Apple iPhone 15",
              "source": "eXtra Stores",
              "price": "SAR 2,749.00",
              "imageUrl": "https://example.com/extra.jpg",
              "link": "https://www.google.com/search?ibp=oshop&prds=extra"
            },
            {
              "title": "Apple iPhone 15 Plus",
              "source": "noon.com",
              "price": "SAR 2,699.00",
              "imageUrl": "https://example.com/noon.jpg",
              "link": "https://www.google.com/search?ibp=oshop&prds=noon"
            },
            {
              "title": "Apple iPhone 15 Black",
              "source": "Amazon",
              "price": "SAR 1,880.00",
              "imageUrl": "https://example.com/amazon-a.jpg",
              "link": "https://www.google.com/search?ibp=oshop&prds=amazon-a"
            },
            {
              "title": "Apple iPhone 15 (Renewed)",
              "source": "Amazon",
              "price": "SAR 1,995.00",
              "imageUrl": "https://example.com/amazon-b.jpg",
              "link": "https://www.google.com/search?ibp=oshop&prds=amazon-b"
            }
          ]
        }
        """;
        var handler = new StubHandler(json);
        var source = new GoogleShoppingDataSource(
            new StubFactory(handler),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        // 3 unique merchants survive: eXtra, Noon, Amazon (deduped to its cheapest).
        Assert.Equal(3, offers.Count);

        var noon = Assert.Single(offers, o => o.StoreName == "Noon");
        Assert.Equal(2699.00m, noon.Price);
        Assert.Equal("SAR", noon.Currency);
        // Noon's URL is our own taggable site-search, not Google's link.
        Assert.StartsWith("https://www.noon.com/saudi-en/search/?q=", noon.ProductUrl);

        var extra = Assert.Single(offers, o => o.StoreName == "eXtra Stores");
        Assert.Equal("https://www.google.com/search?ibp=oshop&prds=extra", extra.ProductUrl);

        // Amazon deduped to the cheaper of its two listings.
        var amazon = Assert.Single(offers, o => o.StoreName == "Amazon");
        Assert.Equal(1880.00m, amazon.Price);
        Assert.Equal("https://www.google.com/search?ibp=oshop&prds=amazon-a", amazon.ProductUrl);

        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("X-API-KEY").Single());
    }

    [Fact]
    public async Task WithoutApiKey_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler("{\"shopping\":[]}");
        var source = new GoogleShoppingDataSource(
            new StubFactory(handler),
            Config(), // no key configured
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        Assert.False(source.IsEnabled);
        Assert.Empty(offers);
        Assert.Null(handler.LastRequest); // never even called the API
    }
}
