using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the Oxylabs live source maps real Amazon.sa API results into
/// StoreOffers correctly and stays dormant with no credentials. The HTTP
/// layer is stubbed so this runs offline; the live connection itself is
/// verified separately once real credentials are configured.
/// </summary>
public class OxylabsAmazonDataSourceTests
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
          "results": [
            {
              "content": {
                "results": {
                  "organic": [
                    {
                      "title": "Samsung Galaxy Watch 7",
                      "price": 849.00,
                      "currency": "SAR",
                      "url": "/Samsung-Galaxy-Watch-7/dp/B0D3XYZ123",
                      "url_image": "https://m.media-amazon.com/images/I/example.jpg",
                      "asin": "B0D3XYZ123",
                      "rating": 4.5
                    }
                  ]
                }
              }
            }
          ]
        }
        """;
        var handler = new StubHandler(json);
        var source = new OxylabsAmazonDataSource(
            new StubFactory(handler),
            Config(
                ("DataSources:Oxylabs:Username", "test-user"),
                ("DataSources:Oxylabs:Password", "test-password")),
            NullLogger<OxylabsAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        Assert.Single(offers);
        var offer = offers[0];
        Assert.Equal("Amazon.sa", offer.StoreName);
        Assert.Equal("Samsung Galaxy Watch 7", offer.ProductTitle);
        Assert.Equal(849.00m, offer.Price);
        Assert.Equal("SAR", offer.Currency);
        Assert.Equal("https://www.amazon.sa/Samsung-Galaxy-Watch-7/dp/B0D3XYZ123", offer.ProductUrl);
        Assert.Equal("https://m.media-amazon.com/images/I/example.jpg", offer.ImageUrl);
        Assert.Equal(4.5m, offer.Rating);

        // Basic auth carries the configured username:password, and the body
        // targets the Saudi marketplace with the search term.
        Assert.NotNull(handler.Request);
        Assert.Equal("Basic", handler.Request!.Headers.Authorization?.Scheme);
        var expectedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-user:test-password"));
        Assert.Equal(expectedAuth, handler.Request!.Headers.Authorization?.Parameter);
        Assert.Contains("samsung galaxy watch 7", handler.RequestBody);
        Assert.Contains("\"domain\":\"sa\"", handler.RequestBody);
    }

    [Fact]
    public async Task WithoutCredentials_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler("{}");
        var source = new OxylabsAmazonDataSource(
            new StubFactory(handler),
            Config(), // no credentials configured
            NullLogger<OxylabsAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        Assert.False(source.IsEnabled);
        Assert.Empty(offers);
        Assert.Null(handler.Request); // never even called the API
    }
}
