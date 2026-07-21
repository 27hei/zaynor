using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the Noon (Apify scraper) live source maps real search results into
/// StoreOffers correctly, parses the actor's loosely-typed price field, and
/// stays dormant with no token. The HTTP layer is stubbed so this runs
/// offline; the live connection itself is verified separately once a real
/// token is configured.
/// </summary>
public class NoonApifyDataSourceTests
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
    public async Task WithApiToken_MapsRealResults_ToStoreOffers()
    {
        const string json = """
        [
          {
            "productName": "Apple iPhone 15 128GB Black",
            "price": "SAR 2,632.20",
            "imageUrl": "https://f.nooncdn.com/p/example.jpg",
            "productUrl": "/saudi-en/iphone-15-128gb/N53432546A/p/",
            "isExpress": true
          },
          {
            "productName": "Sponsored row with no price",
            "price": null,
            "productUrl": "https://www.noon.com/saudi-en/other/p/"
          }
        ]
        """;
        var handler = new StubHandler(json);
        var source = new NoonApifyDataSource(
            new StubFactory(handler),
            Config(("DataSources:NoonApify:ApiToken", "test-token")),
            NullLogger<NoonApifyDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        // The price-less row is dropped; only the real offer survives.
        Assert.Single(offers);
        var offer = offers[0];
        Assert.Equal("Noon", offer.StoreName);
        Assert.Equal("Apple iPhone 15 128GB Black", offer.ProductTitle);
        Assert.Equal(2632.20m, offer.Price); // parsed out of "SAR 2,632.20"
        Assert.Equal("SAR", offer.Currency);
        // A relative productUrl is normalized to an absolute noon.com link.
        Assert.Equal("https://www.noon.com/saudi-en/iphone-15-128gb/N53432546A/p/", offer.ProductUrl);
        Assert.True(offer.FreeShipping);

        Assert.Contains("token=test-token", handler.RequestedUrl);
    }

    [Fact]
    public async Task WithoutApiToken_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler("[]");
        var source = new NoonApifyDataSource(
            new StubFactory(handler),
            Config(), // no token configured
            NullLogger<NoonApifyDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        Assert.False(source.IsEnabled);
        Assert.Empty(offers);
        Assert.Null(handler.RequestedUrl); // never even called the API
    }
}
