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
              "link": "https://www.google.com/search?ibp=oshop&prds=noon",
              "rating": 4.6,
              "ratingCount": 8900
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
        Assert.Equal(4.6m, noon.Rating);
        Assert.Equal(8900, noon.RatingCount);

        var extra = Assert.Single(offers, o => o.StoreName == "eXtra Stores");
        Assert.Equal("https://www.google.com/search?ibp=oshop&prds=extra", extra.ProductUrl);
        Assert.Null(extra.Rating); // no rating field in the stub for this item — never fabricated

        // Amazon deduped to the cheaper of its two listings.
        var amazon = Assert.Single(offers, o => o.StoreName == "Amazon");
        Assert.Equal(1880.00m, amazon.Price);
        Assert.Equal("https://www.google.com/search?ibp=oshop&prds=amazon-a", amazon.ProductUrl);

        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("X-API-KEY").Single());
    }

    [Fact]
    public async Task DiscardsResultsUnrelatedToTheQuery()
    {
        const string json = """
        {
          "shopping": [
            {
              "title": "Sony PlayStation 5 Slim Console",
              "source": "eXtra Stores",
              "price": "SAR 1,999.00",
              "link": "https://www.google.com/search?ibp=oshop&prds=ps5"
            },
            {
              "title": "Samsung 55-inch QLED Smart TV",
              "source": "Amazon",
              "price": "SAR 2,499.00",
              "link": "https://www.google.com/search?ibp=oshop&prds=tv"
            }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("playstation 5");

        // The TV shares no real words with "playstation 5" — a genuine
        // mismatch Google Shopping sometimes mixes in — and must be dropped.
        var offer = Assert.Single(offers);
        Assert.Equal("eXtra Stores", offer.StoreName);
    }

    [Fact]
    public async Task DiscardsCrossBrandModelNumberCollisions()
    {
        // A real observed failure: searching "Samsung A70" also surfaced an
        // unrelated "itel A70" phone — a different brand entirely — because
        // the two titles only need to share the model number under a plain
        // majority rule on a 2-word query. Short brand+model queries must
        // match on every word.
        const string json = """
        {
          "shopping": [
            {
              "title": "Samsung Galaxy A70 128GB",
              "source": "FoneZone.me",
              "price": "SAR 735.00",
              "link": "https://www.google.com/search?ibp=oshop&prds=samsung"
            },
            {
              "title": "itel A70 Android Smartphone",
              "source": "yarmouk-telecom.com",
              "price": "SAR 425.50",
              "link": "https://www.google.com/search?ibp=oshop&prds=itel"
            }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("Samsung A70");

        var offer = Assert.Single(offers);
        Assert.Equal("FoneZone.me", offer.StoreName);
    }

    [Fact]
    public async Task DiscardsRepairPartsAndServicesForTheModelBeingSearched()
    {
        // Real observed failures for the same "Samsung A70" search: a
        // replacement battery, an LCD part, and a screen-assembly repair
        // part all matched on brand+model but are not the phone.
        const string json = """
        {
          "shopping": [
            {
              "title": "Samsung Galaxy A70 EB-BA705ABU 4500mAh battery",
              "source": "Cellspare.com",
              "price": "SAR 23.35",
              "link": "https://www.google.com/search?ibp=oshop&prds=battery"
            },
            {
              "title": "Samsung A705 A70 LCD",
              "source": "eBay",
              "price": "SAR 169.56",
              "link": "https://www.google.com/search?ibp=oshop&prds=lcd"
            },
            {
              "title": "Samsung Galaxy A70 SM-A705 Replacement Screen Assembly",
              "source": "eBay - excellentfixparts",
              "price": "SAR 169.56",
              "link": "https://www.google.com/search?ibp=oshop&prds=screen"
            },
            {
              "title": "Samsung Galaxy A70 Smartphone",
              "source": "eBay - easybu-80",
              "price": "SAR 649.68",
              "link": "https://www.google.com/search?ibp=oshop&prds=phone"
            }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("Samsung A70");

        var offer = Assert.Single(offers);
        Assert.Equal("eBay - easybu-80", offer.StoreName);
    }

    [Fact]
    public async Task DiscardsAccessoriesThatShareWordsWithTheProductBeingSearched()
    {
        // A real observed failure: searching a phone returned its case at a
        // fraction of the price as the "best deal" — the case's title shares
        // enough words with the query ("Samsung", "A70") to pass a plain
        // relevance check, but it is not the phone.
        const string json = """
        {
          "shopping": [
            {
              "title": "Samsung Galaxy A70 Silicone Case Cover",
              "source": "Cellspare.com",
              "price": "SAR 19.33",
              "link": "https://www.google.com/search?ibp=oshop&prds=case"
            },
            {
              "title": "Samsung Galaxy A70 128GB",
              "source": "FoneZone.me",
              "price": "SAR 735.00",
              "link": "https://www.google.com/search?ibp=oshop&prds=phone"
            }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("Samsung A70");

        var offer = Assert.Single(offers);
        Assert.Equal("FoneZone.me", offer.StoreName);
        Assert.Equal(735.00m, offer.Price);
    }

    [Fact]
    public async Task KeepsAccessoriesWhenTheQueryActuallyAsksForOne()
    {
        const string json = """
        {
          "shopping": [
            {
              "title": "Samsung Galaxy A70 Silicone Case Cover",
              "source": "Cellspare.com",
              "price": "SAR 19.33",
              "link": "https://www.google.com/search?ibp=oshop&prds=case"
            }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("Samsung A70 case");

        Assert.Single(offers);
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
