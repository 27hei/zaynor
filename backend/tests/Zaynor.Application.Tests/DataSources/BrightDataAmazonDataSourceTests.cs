using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the Bright Data live source maps real Amazon.sa API results into
/// StoreOffers correctly, polls a still-running snapshot until it's ready,
/// and stays dormant with no API key. The HTTP layer is stubbed so this runs
/// offline; the live trigger+poll cycle itself was verified separately
/// against the real API with real trial credentials.
/// </summary>
public class BrightDataAmazonDataSourceTests
{
    private sealed class StubHandler(
        HttpStatusCode triggerStatus, string triggerBody, params (HttpStatusCode Status, string Body)[] pollResponses)
        : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _pollResponses = new(pollResponses);

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (request.RequestUri!.AbsolutePath.Contains("/trigger"))
            {
                return Task.FromResult(new HttpResponseMessage(triggerStatus)
                {
                    Content = new StringContent(triggerBody, Encoding.UTF8, "application/json"),
                });
            }

            var (status, body) = _pollResponses.Dequeue();
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public async Task WithApiKey_ReadyOnFirstPoll_MapsRealResults()
    {
        const string triggerJson = """{"snapshot_id":"sd_test123"}""";
        const string readyJson = """
        [
          {
            "asin": "B0DGF5BKYS",
            "name": "Samsung Galaxy Watch 7",
            "final_price": 778.90,
            "currency": "SAR",
            "url": "https://www.amazon.sa/dp/B0DGF5BKYS",
            "image": "https://m.media-amazon.com/images/I/example.jpg",
            "rating": 4.7,
            "num_ratings": 72
          }
        ]
        """;
        var handler = new StubHandler(HttpStatusCode.OK, triggerJson, (HttpStatusCode.OK, readyJson));
        var source = new BrightDataAmazonDataSource(
            new StubFactory(handler),
            Config(("DataSources:BrightData:ApiKey", "test-key")),
            NullLogger<BrightDataAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        var offer = Assert.Single(offers);
        Assert.Equal("Amazon.sa", offer.StoreName);
        Assert.Equal("Samsung Galaxy Watch 7", offer.ProductTitle);
        Assert.Equal(778.90m, offer.Price);
        Assert.Equal("SAR", offer.Currency);
        Assert.Equal("https://www.amazon.sa/dp/B0DGF5BKYS", offer.ProductUrl);
        Assert.Equal("https://m.media-amazon.com/images/I/example.jpg", offer.ImageUrl);
        Assert.Equal(4.7m, offer.Rating);
        Assert.Equal(72, offer.RatingCount);

        // trigger + exactly one poll
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("Bearer", handler.Requests[0].Headers.Authorization?.Scheme);
        Assert.Equal("test-key", handler.Requests[0].Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task WithApiKey_KeepsPolling_WhileSnapshotIsStillRunning()
    {
        const string triggerJson = """{"snapshot_id":"sd_test456"}""";
        const string pendingJson = """{"status":"starting","message":"Snapshot is not ready yet, try again in 30s"}""";
        const string readyJson = """
        [
          {"name": "Test Product", "final_price": 100.0, "currency": "SAR", "url": "https://www.amazon.sa/dp/X", "rating": 4.0, "num_ratings": 5}
        ]
        """;
        var handler = new StubHandler(
            HttpStatusCode.OK, triggerJson,
            (HttpStatusCode.Accepted, pendingJson),
            (HttpStatusCode.OK, readyJson));
        var source = new BrightDataAmazonDataSource(
            new StubFactory(handler),
            Config(
                ("DataSources:BrightData:ApiKey", "test-key"),
                ("DataSources:BrightData:PollIntervalMs", "1")), // near-instant so the test doesn't really wait
            NullLogger<BrightDataAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("test product");

        Assert.Single(offers);
        // trigger + one pending poll + one ready poll
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task WithoutApiKey_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var source = new BrightDataAmazonDataSource(
            new StubFactory(handler),
            Config(), // no API key configured
            NullLogger<BrightDataAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        Assert.False(source.IsEnabled);
        Assert.Empty(offers);
        Assert.Empty(handler.Requests); // never even called the API
    }

    [Fact]
    public async Task WithApiKey_TriggerFails_ReturnsEmptyWithoutPolling()
    {
        var handler = new StubHandler(HttpStatusCode.Unauthorized, "{}");
        var source = new BrightDataAmazonDataSource(
            new StubFactory(handler),
            Config(("DataSources:BrightData:ApiKey", "bad-key")),
            NullLogger<BrightDataAmazonDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        Assert.Empty(offers);
        Assert.Single(handler.Requests); // only the trigger call, no polling attempted
    }
}
