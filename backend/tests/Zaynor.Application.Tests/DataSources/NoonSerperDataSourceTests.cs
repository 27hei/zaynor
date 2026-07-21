using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the Noon (Serper/Google Shopping) live source picks out only the
/// Noon-sourced result, maps it to a StoreOffer with a taggable Noon
/// site-search URL (not Google's own compare-prices link), and stays
/// dormant with no key. The HTTP layer is stubbed so this runs offline; the
/// live connection itself is verified separately once a real key is
/// configured.
/// </summary>
public class NoonSerperDataSourceTests
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
    public async Task WithApiKey_PicksOnlyTheNoonResult_AndBuildsATaggableSearchUrl()
    {
        const string json = """
        {
          "shopping": [
            {
              "title": "Apple iPhone 15",
              "source": "eXtra Stores",
              "price": "SAR 2,749.00",
              "imageUrl": "https://example.com/extra.jpg",
              "link": "https://www.google.com/search?ibp=oshop&prds=..."
            },
            {
              "title": "Apple iPhone 15 Plus",
              "source": "noon.com",
              "price": "SAR 2,699.00",
              "imageUrl": "https://example.com/noon.jpg",
              "link": "https://www.google.com/search?ibp=oshop&prds=..."
            }
          ]
        }
        """;
        var handler = new StubHandler(json);
        var source = new NoonSerperDataSource(
            new StubFactory(handler),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<NoonSerperDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        // Only the noon.com row survives — eXtra isn't Noon.
        Assert.Single(offers);
        var offer = offers[0];
        Assert.Equal("Noon", offer.StoreName);
        Assert.Equal("Apple iPhone 15 Plus", offer.ProductTitle);
        Assert.Equal(2699.00m, offer.Price);
        Assert.Equal("SAR", offer.Currency);
        Assert.Equal("https://example.com/noon.jpg", offer.ImageUrl);

        // The outbound URL is our own Noon search (taggable by /api/out),
        // not Google's compare-prices link.
        Assert.StartsWith("https://www.noon.com/saudi-en/search/?q=", offer.ProductUrl);
        Assert.Contains(Uri.EscapeDataString("Apple iPhone 15 Plus"), offer.ProductUrl);

        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("X-API-KEY").Single());
    }

    [Fact]
    public async Task WithoutApiKey_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler("{\"shopping\":[]}");
        var source = new NoonSerperDataSource(
            new StubFactory(handler),
            Config(), // no key configured
            NullLogger<NoonSerperDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        Assert.False(source.IsEnabled);
        Assert.Empty(offers);
        Assert.Null(handler.LastRequest); // never even called the API
    }
}
