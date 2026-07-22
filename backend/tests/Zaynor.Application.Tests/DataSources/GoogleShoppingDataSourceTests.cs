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
        public string? LastRequestBody { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            // The real SearchAsync disposes its HttpRequestMessage (and its
            // Content) before returning, so the body must be captured here,
            // not read from LastRequest afterward.
            LastRequestBody = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
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

        // Amazon deduped to the cheaper of its two listings. Its URL is our
        // own amazon.sa site-search, not Google's fragile compare-prices
        // link — real reported bug: that Google link often shows "no
        // details available for this product" when opened fresh.
        var amazon = Assert.Single(offers, o => o.StoreName == "Amazon");
        Assert.Equal(1880.00m, amazon.Price);
        Assert.StartsWith("https://www.amazon.sa/s?k=", amazon.ProductUrl);

        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("X-API-KEY").Single());
    }

    [Fact]
    public async Task BuildsADirectSiteSearchUrlForRecognizedMerchants_InsteadOfGooglesFragileLink()
    {
        // Real reported bug: clicking through to a store like eBay landed on
        // a blank Google page saying "no details available for this
        // product" — Google's own compare-prices link is a client-side
        // product-panel overlay tied to the search session that generated
        // it, and often fails to resolve when opened fresh. For merchants
        // with a verified, stable site-search pattern, build that instead
        // so the click always lands on a real page.
        const string json = """
        {
          "shopping": [
            { "title": "iPhone 15", "source": "eBay - seller1", "price": "SAR 2000.00", "link": "https://www.google.com/search?ibp=oshop&prds=a" },
            { "title": "iPhone 15", "source": "AliExpress", "price": "SAR 2100.00", "link": "https://www.google.com/search?ibp=oshop&prds=b" },
            { "title": "iPhone 15", "source": "Some Random Boutique Store", "price": "SAR 2200.00", "link": "https://www.google.com/search?ibp=oshop&prds=c" }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        var ebay = Assert.Single(offers, o => o.StoreName == "eBay - seller1");
        Assert.StartsWith("https://www.ebay.com/sch/i.html?_nkw=", ebay.ProductUrl);

        var aliexpress = Assert.Single(offers, o => o.StoreName == "AliExpress");
        Assert.StartsWith("https://www.aliexpress.com/wholesale?SearchText=", aliexpress.ProductUrl);

        // No verified pattern for this one — Google's link remains the
        // fallback, same as before this fix.
        var boutique = Assert.Single(offers, o => o.StoreName == "Some Random Boutique Store");
        Assert.Equal("https://www.google.com/search?ibp=oshop&prds=c", boutique.ProductUrl);
    }

    [Fact]
    public async Task NormalizesColloquialArabicBrandSpellingsBeforeSearching()
    {
        // Real observed failure: "سامسنج A70" (a common colloquial Arabic
        // spelling of Samsung, missing a "و") returned nothing at all from
        // Google Shopping — normalizing to "Samsung" before searching is
        // what makes this query find real results instead of falling back
        // to demo data.
        var handler = new StubHandler("""{"shopping":[]}""");
        var source = new GoogleShoppingDataSource(
            new StubFactory(handler),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        await source.SearchAsync("سامسنج A70");

        Assert.Contains("Samsung A70", handler.LastRequestBody);
        Assert.DoesNotContain("سامسنج", handler.LastRequestBody);
    }

    [Fact]
    public async Task DiscardsArabicTitledRepairPartsForTheModelBeingSearched()
    {
        // Real observed leak: searching "سامسونج A70" (the standard Arabic
        // spelling) returned six offers — a screen protector sticker, a SIM
        // tray, a back cover with camera lens, a battery, and two
        // replacement screens — and not one was the actual phone.
        const string json = """
        {
          "shopping": [
            { "title": "لاصقة حماية للشاشة من الزجاج المقوى لهاتف سامسونج جالاكسي A70 شفاف", "source": "Noon", "price": "SAR 9.00", "link": "https://www.google.com/search?ibp=oshop&prds=a" },
            { "title": "مدخل شريحة سامسونج A70", "source": "نيو كوم", "price": "SAR 19.00", "link": "https://www.google.com/search?ibp=oshop&prds=b" },
            { "title": "غطا خلفي سامسونج A70 مع عدسة الكاميرا", "source": "جوال اكسبريس", "price": "SAR 25.00", "link": "https://www.google.com/search?ibp=oshop&prds=c" },
            { "title": "بطارية سامسونج A70", "source": "شاشة ستور", "price": "SAR 60.00", "link": "https://www.google.com/search?ibp=oshop&prds=d" },
            { "title": "شاشة سامسونج A70 5G", "source": "salla.sa", "price": "SAR 310.00", "link": "https://www.google.com/search?ibp=oshop&prds=e" },
            { "title": "سامسونج جالكسي A70 128 جيجابايت", "source": "Jarir", "price": "SAR 735.00", "link": "https://www.google.com/search?ibp=oshop&prds=f" }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("سامسونج A70");

        var offer = Assert.Single(offers);
        Assert.Equal("Jarir", offer.StoreName);
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
    public async Task DiscardsListingsPricedAsOutliersAgainstTheGenuineCluster()
    {
        // A real observed gap: searching "iPhone 15" returned a "phone" at 20
        // SAR and a RhinoShield case listed only as "iPhone 15 Mod NX Black"
        // — neither title contains an accessory keyword, so only the price
        // (a small fraction of every genuine listing) gives it away.
        const string json = """
        {
          "shopping": [
            { "title": "Iphone 15 pro", "source": "gpsaudi.com", "price": "SAR 20.00", "link": "https://www.google.com/search?ibp=oshop&prds=a" },
            { "title": "iPhone 15 Mod NX Black", "source": "rhinoshield.io", "price": "SAR 125.77", "link": "https://www.google.com/search?ibp=oshop&prds=b" },
            { "title": "Apple iPhone 15 A16 Bionic", "source": "FoneZone.me", "price": "SAR 1965.06", "link": "https://www.google.com/search?ibp=oshop&prds=c" },
            { "title": "Apple iPhone 15 128GB", "source": "Amazon.sa", "price": "SAR 2561.00", "link": "https://www.google.com/search?ibp=oshop&prds=d" },
            { "title": "Apple iPhone 15", "source": "Jarir Bookstore", "price": "SAR 2799.00", "link": "https://www.google.com/search?ibp=oshop&prds=e" }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        Assert.Equal(3, offers.Count);
        Assert.DoesNotContain(offers, o => o.StoreName == "gpsaudi.com");
        Assert.DoesNotContain(offers, o => o.StoreName == "rhinoshield.io");
        Assert.Contains(offers, o => o.StoreName == "FoneZone.me");
    }

    [Fact]
    public async Task DiscardsListingsPricedWildlyAboveTheGenuineCluster()
    {
        // Real reported case: searching a personal-care product once
        // returned a listing priced in the thousands of SAR against a
        // genuine cluster of tens of SAR (the real product costs ~80 SAR on
        // Amazon) — the symmetric counterpart to the "too cheap" outlier
        // filter above. 8x the median comfortably keeps a legitimate
        // premium variant while catching an order-of-magnitude mismatch.
        const string json = """
        {
          "shopping": [
            { "title": "Nip and Fab Cleanser 200ml", "source": "Nahdi Online", "price": "SAR 38.00", "link": "https://www.google.com/search?ibp=oshop&prds=a" },
            { "title": "Nip and Fab Cleanser 150ml", "source": "Henzadem", "price": "SAR 45.00", "link": "https://www.google.com/search?ibp=oshop&prds=b" },
            { "title": "Nip and Fab Cleanser", "source": "Amazon", "price": "SAR 80.00", "link": "https://www.google.com/search?ibp=oshop&prds=c" },
            { "title": "Nip and Fab Cleanser Bundle", "source": "SuspiciousStore", "price": "SAR 4000.00", "link": "https://www.google.com/search?ibp=oshop&prds=d" }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("nip and fab cleanser");

        Assert.Equal(3, offers.Count);
        Assert.DoesNotContain(offers, o => o.StoreName == "SuspiciousStore");
        Assert.Contains(offers, o => o.StoreName == "Amazon");
    }

    [Fact]
    public async Task DiscardsMerchBundlesThatArentThePlayedDeviceItself()
    {
        // A real observed leak: searching "Xbox Series X" surfaced a
        // "T-Shirt & Controller" bundle at a price low enough to survive
        // the price-outlier filter, but not the actual console. Bare
        // "controller" isn't excluded (a genuine "Console with Wireless
        // Controller" bundle would wrongly be caught by that), but the
        // apparel mention is an unambiguous signal.
        const string json = """
        {
          "shopping": [
            {
              "title": "Galaxy Bundle – T-Shirt & Xbox Series X Controller",
              "source": "DreamController",
              "price": "SAR 763.00",
              "link": "https://www.google.com/search?ibp=oshop&prds=bundle"
            },
            {
              "title": "Microsoft Xbox Series X Console",
              "source": "dokan-ps",
              "price": "SAR 2832.68",
              "link": "https://www.google.com/search?ibp=oshop&prds=console"
            },
            {
              "title": "Xbox Series X 1TB with Wireless Controller",
              "source": "desertcart.co.za",
              "price": "SAR 6195.24",
              "link": "https://www.google.com/search?ibp=oshop&prds=bundle2"
            }
          ]
        }
        """;
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(json)),
            Config(("DataSources:Serper:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("Xbox Series X");

        Assert.DoesNotContain(offers, o => o.StoreName == "DreamController");
        // A genuine console+controller bundle must still survive.
        Assert.Contains(offers, o => o.StoreName == "desertcart.co.za");
        Assert.Contains(offers, o => o.StoreName == "dokan-ps");
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
