using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.ImageSearch;

namespace Zaynor.Application.Tests.ImageSearch;

public class SerperLensQueryResolverTests
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
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public async Task WithApiKey_ReturnsTheTopVisualMatchTitle()
    {
        const string json = """
        {
          "organic": [
            { "title": "Apple iPhone 15 Plus" },
            { "title": "Some other visually-similar thing" }
          ]
        }
        """;
        var handler = new StubHandler(json);
        var resolver = new SerperLensQueryResolver(
            new StubFactory(handler),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<SerperLensQueryResolver>.Instance);

        var query = await resolver.ResolveQueryAsync("https://example.com/photo.jpg");

        Assert.Equal("Apple iPhone 15 Plus", query);
        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("X-API-KEY").Single());
    }

    [Fact]
    public async Task WithNoOrganicResults_ReturnsNull()
    {
        var resolver = new SerperLensQueryResolver(
            new StubFactory(new StubHandler("{\"organic\":[]}")),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<SerperLensQueryResolver>.Instance);

        Assert.Null(await resolver.ResolveQueryAsync("https://example.com/photo.jpg"));
    }

    [Fact]
    public async Task WithoutApiKey_IsDormant_AndNeverCallsTheApi()
    {
        var handler = new StubHandler("{\"organic\":[]}");
        var resolver = new SerperLensQueryResolver(
            new StubFactory(handler),
            Config(), // no key configured
            NullLogger<SerperLensQueryResolver>.Instance);

        var query = await resolver.ResolveQueryAsync("https://example.com/photo.jpg");

        Assert.False(resolver.IsEnabled);
        Assert.Null(query);
        Assert.Null(handler.LastRequest);
    }
}
