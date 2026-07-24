using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the HasData Google Shopping source maps real per-merchant results
/// (including HasData's Arabic-formatted price strings) into StoreOffers,
/// dedupes per merchant, and stays dormant with no key. Precision filtering
/// (accessory/repair-part/outlier/relevance) is shared with
/// GoogleShoppingDataSource via ListingRelevanceFilter and already fully
/// covered by GoogleShoppingDataSourceTests — not re-tested here beyond one
/// wiring check. The HTTP layer is stubbed so this runs offline; the live
/// connection itself was verified separately against the real API (real
/// Saudi Arabia query, gl=sa/hl=en, returned stc/Amazon.sa/Microless.com/eBay).
/// </summary>
public class HasDataShoppingDataSourceTests
{
    /// <summary>
    /// Routes by URL: the base shopping-search call always gets
    /// <paramref name="shoppingJson"/>; any other request (the
    /// immersive-product expansion, whose URL is the "hasdataLink" from the
    /// shopping response) gets <paramref name="immersiveJson"/>.
    /// </summary>
    private sealed class StubHandler(string shoppingJson, string immersiveJson) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var json = request.RequestUri!.ToString().Contains("/scrape/google/shopping") ? shoppingJson : immersiveJson;
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

    private const string ShoppingJson = """
        {
          "shoppingResults": [
            { "title": "Samsung Galaxy Watch 7", "thumbnail": "https://example.com/thumb.jpg", "hasdataLink": "https://api.hasdata.com/scrape/google/immersive-product?pageToken=t1" }
          ]
        }
        """;

    [Fact]
    public async Task WithApiKey_MapsRealMerchants_ParsingArabicFormattedPrices()
    {
        // Real shape (verified live): price/total are RTL Arabic strings
        // with Arabic-Indic digits, e.g. "‏٧٧٢٫٣٤ ر.س.‏" = 772.34 SAR.
        const string immersiveJson = """
        {
          "productResults": {
            "title": "Samsung ساعة سامسونج",
            "brand": "Samsung",
            "stores": [
              {
                "name": "stc",
                "link": "https://www.stc.com.sa/product-detail?productId=P001308",
                "title": "Galaxy Watch 7 Green",
                "price": "‏٥٩٩٫٠٠ ر.س.‏",
                "shipping": "مجانًا",
                "total": "‏٥٩٩٫٠٠ ر.س.‏",
                "detailsAndOffers": ["التوصيل مجاني", "المنتج متوفّر"]
              },
              {
                "name": "Amazon",
                "link": "https://www.amazon.sa/dp/B0F2H1VR5M",
                "title": "Samsung Galaxy Watch 7 40mm",
                "price": "‏٧٧٢٫٣٤ ر.س.‏",
                "total": "‏٧٧٢٫٣٤ ر.س.‏",
                "rating": 4.4,
                "reviews": 1087
              }
            ]
          }
        }
        """;
        var handler = new StubHandler(ShoppingJson, immersiveJson);
        var source = new HasDataShoppingDataSource(
            new StubFactory(handler),
            Config(("DataSources:HasData:ApiKey", "test-key")),
            NullLogger<HasDataShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        Assert.Equal(2, offers.Count);

        var stc = Assert.Single(offers, o => o.StoreName == "stc");
        Assert.Equal(599.00m, stc.Price);
        Assert.Equal("SAR", stc.Currency);
        Assert.Equal("https://www.stc.com.sa/product-detail?productId=P001308", stc.ProductUrl);
        Assert.True(stc.FreeShipping); // "مجانًا" = free
        Assert.Equal(2, stc.ProductDetails!.StoreHighlights!.Count);

        var amazon = Assert.Single(offers, o => o.StoreName == "Amazon");
        Assert.Equal(772.34m, amazon.Price);
        Assert.Equal(4.4m, amazon.Rating);
        Assert.Equal(1087, amazon.RatingCount);

        // Real API key propagates as the x-api-key header on both calls.
        Assert.All(handler.Requests, r => Assert.Equal("test-key", r.Headers.GetValues("x-api-key").Single()));
        Assert.Contains(handler.Requests, r => r.RequestUri!.ToString().Contains("gl=sa") && r.RequestUri.ToString().Contains("hl=en"));
    }

    [Fact]
    public async Task DedupesRepeatListingsFromTheSameMerchant_KeepingTheCheapest()
    {
        const string immersiveJson = """
        {
          "productResults": {
            "stores": [
              { "name": "Amazon", "link": "https://www.amazon.sa/a", "title": "Samsung Galaxy Watch 7 Black", "price": "٣٠٠ ر.س." },
              { "name": "Amazon", "link": "https://www.amazon.sa/b", "title": "Samsung Galaxy Watch 7 Green", "price": "٢٥٠ ر.س." }
            ]
          }
        }
        """;
        var source = new HasDataShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson, immersiveJson)),
            Config(("DataSources:HasData:ApiKey", "test-key")),
            NullLogger<HasDataShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        var amazon = Assert.Single(offers);
        Assert.Equal(250m, amazon.Price);
        Assert.Equal("https://www.amazon.sa/b", amazon.ProductUrl);
    }

    [Fact]
    public async Task RenamesAnyNoonVariantSpelling_ToTheCanonicalStoreName()
    {
        const string immersiveJson = """
        {
          "productResults": {
            "stores": [
              { "name": "noon.com", "link": "https://www.noon.com/saudi-en/x", "title": "Samsung Galaxy Watch 7", "price": "٤٥٠ ر.س." }
            ]
          }
        }
        """;
        var source = new HasDataShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson, immersiveJson)),
            Config(("DataSources:HasData:ApiKey", "test-key")),
            NullLogger<HasDataShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        Assert.Equal("Noon", Assert.Single(offers).StoreName);
    }

    [Fact]
    public async Task DiscardsAnAccessoryListing_ReusingTheSharedRelevanceFilter()
    {
        // Confirms this source is actually wired to ListingRelevanceFilter —
        // the filter's own behavior is exhaustively covered by
        // GoogleShoppingDataSourceTests, not repeated here.
        const string immersiveJson = """
        {
          "productResults": {
            "stores": [
              { "name": "CaseStore", "link": "https://example.com/case", "title": "Samsung Galaxy Watch 7 Silicone Case Cover", "price": "٢٠ ر.س." },
              { "name": "Amazon", "link": "https://www.amazon.sa/watch", "title": "Samsung Galaxy Watch 7", "price": "٦٠٠ ر.س." }
            ]
          }
        }
        """;
        var source = new HasDataShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson, immersiveJson)),
            Config(("DataSources:HasData:ApiKey", "test-key")),
            NullLogger<HasDataShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        var offer = Assert.Single(offers);
        Assert.Equal("Amazon", offer.StoreName);
    }

    [Fact]
    public async Task WithoutApiKey_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler(ShoppingJson, """{"productResults":{"stores":[]}}""");
        var source = new HasDataShoppingDataSource(
            new StubFactory(handler),
            Config(), // no key configured
            NullLogger<HasDataShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("samsung galaxy watch 7");

        Assert.False(source.IsEnabled);
        Assert.Empty(offers);
        Assert.Empty(handler.Requests); // never even called the API
    }
}
