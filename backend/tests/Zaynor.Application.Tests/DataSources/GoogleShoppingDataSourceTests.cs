using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Application.Tests.DataSources;

/// <summary>
/// Proves the open-scope Google Shopping (SerpApi) source maps every real
/// merchant into a StoreOffer (not just Noon), dedupes repeat listings from
/// the same merchant down to its cheapest, passes through each store's own
/// direct product-page link from the Immersive Product API untouched (real
/// reported bug this replaced: Google's flat Shopping "link" field is a
/// fragile session-tied panel that often shows "no details available"), and
/// stays dormant with no key. The HTTP layer is stubbed so this runs offline.
/// </summary>
public class GoogleShoppingDataSourceTests
{
    /// <summary>
    /// Routes by URL: the base <c>engine=google_shopping</c> call always gets
    /// <paramref name="shoppingJson"/>; each <c>engine=google_immersive_product</c>
    /// call is matched by whichever token appears in its <c>page_token</c>,
    /// looked up in <paramref name="immersiveJsonByToken"/>.
    /// </summary>
    private sealed class StubHandler(string shoppingJson, Dictionary<string, string>? immersiveJsonByToken = null) : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _immersive = immersiveJsonByToken ?? [];

        public HttpRequestMessage? LastRequest { get; private set; }
        public List<string> RequestedUrls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var url = request.RequestUri!.ToString();
            RequestedUrls.Add(url);

            string json;
            if (url.Contains("engine=google_immersive_product"))
            {
                var match = _immersive.FirstOrDefault(kv => url.Contains(kv.Key));
                json = match.Value ?? """{"product_results":{"stores":[]}}""";
            }
            else
            {
                json = shoppingJson;
            }

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

    /// <summary>A shopping_results envelope with a single candidate product carrying the given token.</summary>
    private static string ShoppingJson(string token, string title = "Apple iPhone 15") => $$"""
        {
          "shopping_results": [
            { "title": "{{title}}", "thumbnail": "https://example.com/thumb.jpg", "immersive_product_page_token": "{{token}}" }
          ]
        }
        """;

    /// <summary>A product_results.stores envelope for one immersive-expansion response.</summary>
    private static string StoresJson(params string[] storeJsonEntries) =>
        "{\"product_results\":{\"stores\":[" + string.Join(",", storeJsonEntries) + "]}}";

    /// <summary>A product_results envelope with product-level detail fields (images/brand/description/features) alongside its stores.</summary>
    private static string RichProductResultsJson(
        string[] thumbnails, string brand, string aboutDescription, (string Title, string Value)[] features, params string[] storeJsonEntries)
    {
        var thumbsJson = string.Join(",", thumbnails.Select(t => $"\"{t}\""));
        var featuresJson = string.Join(",", features.Select(f => $$"""{"title":"{{f.Title}}","value":"{{f.Value}}"}"""));
        return $$"""
            {
              "product_results": {
                "thumbnails": [{{thumbsJson}}],
                "brand": "{{brand}}",
                "about_the_product": { "description": "{{aboutDescription}}", "features": [{{featuresJson}}] },
                "stores": [{{string.Join(",", storeJsonEntries)}}]
              }
            }
            """;
    }

    private static string Store(string name, string link, decimal price, string? title = null, double? rating = null, int? reviews = null, string[]? detailsAndOffers = null)
    {
        var ratingPart = rating is { } r ? $""", "rating": {r}""" : "";
        var reviewsPart = reviews is { } rv ? $""", "reviews": {rv}""" : "";
        var detailsPart = detailsAndOffers is { Length: > 0 } d
            ? $""", "details_and_offers": [{string.Join(",", d.Select(x => $"\"{x}\""))}]"""
            : "";
        return $$"""
            {
              "name": "{{name}}",
              "title": "{{title ?? name}}",
              "link": "{{link}}",
              "price": "SAR {{price}}",
              "extracted_price": {{price}}{{ratingPart}}{{reviewsPart}}{{detailsPart}}
            }
            """;
    }

    [Fact]
    public async Task WithApiKey_ReturnsEveryMerchant_DedupedAndMapped_WithRealDirectLinks()
    {
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("eXtra Stores", "https://www.extra.com/en-sa/real-iphone-15-page", 2749.00m, "Apple iPhone 15"),
                Store("noon.com", "https://www.noon.com/saudi-en/real-iphone-15-page", 2699.00m, "Apple iPhone 15 Plus", rating: 4.6, reviews: 8900),
                Store("Amazon", "https://www.amazon.sa/-/en/real-iphone-15-a", 1880.00m, "Apple iPhone 15 Black"),
                Store("Amazon", "https://www.amazon.sa/-/en/real-iphone-15-b", 1995.00m, "Apple iPhone 15 (Renewed)")),
        };
        var handler = new StubHandler(ShoppingJson("t1"), immersive);
        var source = new GoogleShoppingDataSource(
            new StubFactory(handler),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        // 3 unique merchants survive: eXtra, Noon, Amazon (deduped to its cheapest).
        Assert.Equal(3, offers.Count);

        var noon = Assert.Single(offers, o => o.StoreName == "Noon");
        Assert.Equal(2699.00m, noon.Price);
        Assert.Equal("SAR", noon.Currency);
        // Passed through verbatim from the Immersive Product API — no
        // guessed/constructed URL, this is the store's own real page.
        Assert.Equal("https://www.noon.com/saudi-en/real-iphone-15-page", noon.ProductUrl);
        Assert.Equal(4.6m, noon.Rating);
        Assert.Equal(8900, noon.RatingCount);

        var extra = Assert.Single(offers, o => o.StoreName == "eXtra Stores");
        Assert.Equal("https://www.extra.com/en-sa/real-iphone-15-page", extra.ProductUrl);
        Assert.Null(extra.Rating); // no rating field in the stub for this item — never fabricated

        // Amazon deduped to the cheaper of its two listings.
        var amazon = Assert.Single(offers, o => o.StoreName == "Amazon");
        Assert.Equal(1880.00m, amazon.Price);
        Assert.Equal("https://www.amazon.sa/-/en/real-iphone-15-a", amazon.ProductUrl);

        Assert.Contains(handler.RequestedUrls, u => u.Contains("api_key=test-key") && u.Contains("engine=google_shopping"));
        Assert.Contains(handler.RequestedUrls, u => u.Contains("api_key=test-key") && u.Contains("engine=google_immersive_product"));
    }

    [Fact]
    public async Task PassesThroughEachStoresOwnLinkVerbatim_NoGuessedUrlPatterns()
    {
        // Real reported bug this replaced: clicking through to a store like
        // eBay used to land on a blank Google page ("no details available
        // for this product") because the old flat Shopping "link" field is a
        // fragile session-tied panel. The Immersive Product API's per-store
        // "link" is the real product page, so it's used exactly as given —
        // for every merchant, not just a hardcoded few.
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("eBay - seller1", "https://www.ebay.com/itm/real-listing-123", 2000.00m, "Apple iPhone 15 128GB"),
                Store("AliExpress", "https://www.aliexpress.com/item/real-listing-456.html", 2100.00m, "Apple iPhone 15"),
                Store("Some Random Boutique Store", "https://someboutique.example.com/products/iphone-15", 2200.00m, "Apple iPhone 15")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        var ebay = Assert.Single(offers, o => o.StoreName == "eBay - seller1");
        Assert.Equal("https://www.ebay.com/itm/real-listing-123", ebay.ProductUrl);

        var aliexpress = Assert.Single(offers, o => o.StoreName == "AliExpress");
        Assert.Equal("https://www.aliexpress.com/item/real-listing-456.html", aliexpress.ProductUrl);

        var boutique = Assert.Single(offers, o => o.StoreName == "Some Random Boutique Store");
        Assert.Equal("https://someboutique.example.com/products/iphone-15", boutique.ProductUrl);
    }

    [Fact]
    public async Task WithRichImmersiveFields_PopulatesProductDetails_PerStore()
    {
        // The Immersive Product API response (already fetched for the direct
        // link above — no extra paid call) carries rich fields Zaynor was
        // discarding: product-level images/brand/description/specs, and each
        // store's own fulfillment bullets ("In stock online", "Delivery SAR
        // 29", ...). Captured now so a product-detail page can show them.
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = RichProductResultsJson(
                thumbnails: ["https://example.com/img1.jpg", "https://example.com/img2.jpg"],
                brand: "Apple",
                aboutDescription: "iPhone 15 brings Dynamic Island and a 48MP camera.",
                features: [("Processor", "A16 Bionic"), ("Water Resistant", "Yes")],
                storeJsonEntries:
                [
                    Store("Jarir Bookstore", "https://www.jarir.com/real-iphone-15", 2799.00m, "Apple iPhone 15",
                        detailsAndOffers: ["In stock online", "Delivery SAR 29", "3-day returns"]),
                ]),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        var offer = Assert.Single(offers);
        Assert.NotNull(offer.ProductDetails);
        Assert.Equal(["https://example.com/img1.jpg", "https://example.com/img2.jpg"], offer.ProductDetails!.Images);
        Assert.Equal("Apple", offer.ProductDetails.Brand);
        Assert.Equal("iPhone 15 brings Dynamic Island and a 48MP camera.", offer.ProductDetails.Description);
        Assert.Equal(["Processor: A16 Bionic", "Water Resistant: Yes"], offer.ProductDetails.Specifications);
        Assert.Equal(["In stock online", "Delivery SAR 29", "3-day returns"], offer.ProductDetails.StoreHighlights);
    }

    [Fact]
    public async Task WithoutRichImmersiveFields_ProductDetailsIsNull()
    {
        // No all-null wrapper object when nothing usable was actually
        // returned — a source with no rich data must not fabricate one.
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(Store("Jarir Bookstore", "https://www.jarir.com/real-iphone-15", 2799.00m, "Apple iPhone 15")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        var offer = Assert.Single(offers);
        Assert.Null(offer.ProductDetails);
    }

    [Fact]
    public async Task SignsEveryOffersUrl_SoOutControllerCanTrustDomainsOutsideItsStaticList()
    {
        // /api/out has a static list of known store domains it'll redirect
        // to; the whole reason this source needed a signature at all is that
        // real merchants resolved live via the Immersive Product API
        // (Mazeed, LetsTango, desertcart, ...) are an open-ended set that
        // list can never fully cover. Each offer's URL is signed with the
        // same key OutController verifies against.
        const string signingKey = "test-jwt-signing-key-0123456789";
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(Store("Mazeed", "https://mazeed.sa/products/real-listing", 2200.00m, "Apple iPhone 15")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key"), ("Jwt:Key", signingKey)),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("iphone 15");

        var offer = Assert.Single(offers);
        Assert.NotNull(offer.Signature);
        Assert.True(Zaynor.Application.Aggregation.OutboundLinkSigner.Verify(offer.ProductUrl, offer.Signature, signingKey));
    }

    [Fact]
    public async Task NormalizesColloquialArabicBrandSpellingsBeforeSearching()
    {
        // Real observed failure: "سامسنج A70" (a common colloquial Arabic
        // spelling of Samsung, missing a "و") returned nothing at all from
        // Google Shopping — normalizing to "Samsung" before searching is
        // what makes this query find real results instead of falling back
        // to demo data.
        var handler = new StubHandler("""{"shopping_results":[]}""");
        var source = new GoogleShoppingDataSource(
            new StubFactory(handler),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        await source.SearchAsync("سامسنج A70");

        var shoppingUrl = Assert.Single(handler.RequestedUrls, u => u.Contains("engine=google_shopping"));
        Assert.Contains("Samsung", Uri.UnescapeDataString(shoppingUrl));
        Assert.DoesNotContain("سامسنج", Uri.UnescapeDataString(shoppingUrl));
    }

    [Fact]
    public async Task DiscardsArabicTitledRepairPartsForTheModelBeingSearched()
    {
        // Real observed leak: searching "سامسونج A70" (the standard Arabic
        // spelling) returned six offers — a screen protector sticker, a SIM
        // tray, a back cover with camera lens, a battery, and two
        // replacement screens — and not one was the actual phone.
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("Noon", "https://noon.example/a", 9.00m, "لاصقة حماية للشاشة من الزجاج المقوى لهاتف سامسونج جالاكسي A70 شفاف"),
                Store("نيو كوم", "https://newcom.example/b", 19.00m, "مدخل شريحة سامسونج A70"),
                Store("جوال اكسبريس", "https://mobile-express.example/c", 25.00m, "غطا خلفي سامسونج A70 مع عدسة الكاميرا"),
                Store("شاشة ستور", "https://screenstore.example/d", 60.00m, "بطارية سامسونج A70"),
                Store("salla.sa", "https://salla.example/e", 310.00m, "شاشة سامسونج A70 5G"),
                Store("Jarir", "https://jarir.example/f", 735.00m, "سامسونج جالكسي A70 128 جيجابايت")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("سامسونج A70");

        var offer = Assert.Single(offers);
        Assert.Equal("Jarir", offer.StoreName);
    }

    [Fact]
    public async Task DiscardsResultsUnrelatedToTheQuery()
    {
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("eXtra Stores", "https://extra.example/ps5", 1999.00m, "Sony PlayStation 5 Slim Console"),
                Store("Amazon", "https://amazon.example/tv", 2499.00m, "Samsung 55-inch QLED Smart TV")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
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
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("FoneZone.me", "https://fonezone.example/samsung", 735.00m, "Samsung Galaxy A70 128GB"),
                Store("yarmouk-telecom.com", "https://yarmouk.example/itel", 425.50m, "itel A70 Android Smartphone")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
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
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("Cellspare.com", "https://cellspare.example/battery", 23.35m, "Samsung Galaxy A70 EB-BA705ABU 4500mAh battery"),
                Store("eBay", "https://ebay.example/lcd", 169.56m, "Samsung A705 A70 LCD"),
                Store("eBay - excellentfixparts", "https://ebay.example/screen", 169.56m, "Samsung Galaxy A70 SM-A705 Replacement Screen Assembly"),
                Store("eBay - easybu-80", "https://ebay.example/phone", 649.68m, "Samsung Galaxy A70 Smartphone")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
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
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("gpsaudi.com", "https://gpsaudi.example/a", 20.00m, "Iphone 15 pro"),
                Store("rhinoshield.io", "https://rhinoshield.example/b", 125.77m, "iPhone 15 Mod NX Black"),
                Store("FoneZone.me", "https://fonezone.example/c", 1965.06m, "Apple iPhone 15 A16 Bionic"),
                Store("Amazon.sa", "https://amazon.example/d", 2561.00m, "Apple iPhone 15 128GB"),
                Store("Jarir Bookstore", "https://jarir.example/e", 2799.00m, "Apple iPhone 15")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
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
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("Nahdi Online", "https://nahdi.example/a", 38.00m, "Nip and Fab Cleanser 200ml"),
                Store("Henzadem", "https://henzadem.example/b", 45.00m, "Nip and Fab Cleanser 150ml"),
                Store("Amazon", "https://amazon.example/c", 80.00m, "Nip and Fab Cleanser"),
                Store("SuspiciousStore", "https://suspicious.example/d", 4000.00m, "Nip and Fab Cleanser Bundle")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
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
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("DreamController", "https://dreamcontroller.example/bundle", 763.00m, "Galaxy Bundle – T-Shirt & Xbox Series X Controller"),
                Store("dokan-ps", "https://dokan.example/console", 2832.68m, "Microsoft Xbox Series X Console"),
                Store("desertcart.co.za", "https://desertcart.example/bundle2", 6195.24m, "Xbox Series X 1TB with Wireless Controller")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
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
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(
                Store("Cellspare.com", "https://cellspare.example/case", 19.33m, "Samsung Galaxy A70 Silicone Case Cover"),
                Store("FoneZone.me", "https://fonezone.example/phone", 735.00m, "Samsung Galaxy A70 128GB")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("Samsung A70");

        var offer = Assert.Single(offers);
        Assert.Equal("FoneZone.me", offer.StoreName);
        Assert.Equal(735.00m, offer.Price);
    }

    [Fact]
    public async Task KeepsAccessoriesWhenTheQueryActuallyAsksForOne()
    {
        var immersive = new Dictionary<string, string>
        {
            ["t1"] = StoresJson(Store("Cellspare.com", "https://cellspare.example/case", 19.33m, "Samsung Galaxy A70 Silicone Case Cover")),
        };
        var source = new GoogleShoppingDataSource(
            new StubFactory(new StubHandler(ShoppingJson("t1"), immersive)),
            Config(("DataSources:SerpApi:ApiKey", "test-key")),
            NullLogger<GoogleShoppingDataSource>.Instance);

        var offers = await source.SearchAsync("Samsung A70 case");

        Assert.Single(offers);
    }

    [Fact]
    public async Task WithoutApiKey_IsDormant_AndReturnsNothing()
    {
        var handler = new StubHandler("""{"shopping_results":[]}""");
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
