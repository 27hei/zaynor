using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Tests.Aggregation;

public class AggregationServiceTests
{
    private static AggregationService CreateService(params IProductDataSource[] sources) =>
        new(sources, new AffiliateSettings(), NullLogger<AggregationService>.Instance);

    private static AggregationService CreateService(AffiliateSettings affiliateSettings, params IProductDataSource[] sources) =>
        new(sources, affiliateSettings, NullLogger<AggregationService>.Instance);

    [Fact]
    public async Task SearchAsync_SurfacesTheCorrectedQuery_WhenAColloquialArabicBrandSpellingWasFixed()
    {
        var service = CreateService(FakeDataSource.Returning(FakeDataSource.Offer("Jarir", 2000m)));

        var result = await service.SearchAsync("سامسنج A70");

        Assert.Equal("Samsung A70", result.CorrectedQuery);
    }

    [Fact]
    public async Task SearchAsync_CorrectedQueryIsNull_WhenNothingNeededCorrecting()
    {
        var service = CreateService(FakeDataSource.Returning(FakeDataSource.Offer("Jarir", 2000m)));

        var result = await service.SearchAsync("Samsung A70");

        Assert.Null(result.CorrectedQuery);
    }

    [Fact]
    public async Task SearchAsync_PassesProductDetailsThroughUnchanged()
    {
        var details = new ProductDetails
        {
            Images = ["https://example.com/img.jpg"],
            Brand = "Apple",
            Description = "A phone.",
            Specifications = ["Processor: A16"],
            StoreHighlights = ["In stock online"],
        };
        var service = CreateService(FakeDataSource.Returning(FakeDataSource.Offer("Jarir", 2000m, details)));

        var result = await service.SearchAsync("iphone 15");

        Assert.Same(details, Assert.Single(result.Offers).ProductDetails);
    }

    [Fact]
    public async Task SearchAsync_ProductDetailsIsNull_WhenTheSourceDidntProvideAny()
    {
        var service = CreateService(FakeDataSource.Returning(FakeDataSource.Offer("Jarir", 2000m)));

        var result = await service.SearchAsync("iphone 15");

        Assert.Null(Assert.Single(result.Offers).ProductDetails);
    }

    [Fact]
    public async Task SearchAsync_FlagsOffers_OnlyForStoresWithAnActiveAffiliateConfig()
    {
        var noonOffer = new StoreOffer
        {
            StoreName = "Noon",
            ProductTitle = "Test Product",
            Price = 100m,
            Currency = "SAR",
            ProductUrl = "https://www.noon.com/saudi-en/search/?q=test",
        };
        var jarirOffer = new StoreOffer
        {
            StoreName = "Jarir",
            ProductTitle = "Test Product",
            Price = 200m,
            Currency = "SAR",
            ProductUrl = "https://www.jarir.com/product",
        };
        var settings = new AffiliateSettings { NoonSuffixConfigured = true }; // Jarir/deeplink NOT configured
        var service = CreateService(settings, FakeDataSource.Returning(noonOffer, jarirOffer));

        var result = await service.SearchAsync("test");

        Assert.True(result.Offers.Single(o => o.StoreName == "Noon").HasAffiliateLink);
        Assert.False(result.Offers.Single(o => o.StoreName == "Jarir").HasAffiliateLink);
    }

    [Fact]
    public async Task SearchAsync_SortsOffersByPriceAscending()
    {
        var service = CreateService(FakeDataSource.Returning(
            FakeDataSource.Offer("Noon", 5000m),
            FakeDataSource.Offer("Amazon.sa", 4000m),
            FakeDataSource.Offer("Jarir", 4500m)));

        var result = await service.SearchAsync("ps5");

        Assert.Equal(new[] { 4000m, 4500m, 5000m }, result.Offers.Select(o => o.Price));
    }

    [Fact]
    public async Task SearchAsync_FlagsOnlyTheCheapestOfferAsLowest()
    {
        var service = CreateService(FakeDataSource.Returning(
            FakeDataSource.Offer("Noon", 5000m),
            FakeDataSource.Offer("Amazon.sa", 4000m)));

        var result = await service.SearchAsync("ps5");

        Assert.Equal("Amazon.sa", result.Offers.Single(o => o.IsLowestPrice).StoreName);
        Assert.Single(result.Offers, o => o.IsLowestPrice);
    }

    [Fact]
    public async Task SearchAsync_BuildsRecommendationWithSavingsVersusMostExpensive()
    {
        var service = CreateService(FakeDataSource.Returning(
            FakeDataSource.Offer("Noon", 5000m),
            FakeDataSource.Offer("Amazon.sa", 4000m)));

        var result = await service.SearchAsync("ps5");

        Assert.NotNull(result.Recommendation);
        Assert.Equal("Amazon.sa", result.Recommendation!.BestStoreName);
        Assert.Equal(4000m, result.Recommendation.BestPrice);
        Assert.Equal("Noon", result.Recommendation.ComparedStoreName);
        Assert.Equal(1000m, result.Recommendation.Savings);
    }

    [Fact]
    public async Task SearchAsync_MergesOffersFromMultipleSources()
    {
        var service = CreateService(
            FakeDataSource.Returning(FakeDataSource.Offer("Amazon.sa", 4000m)),
            FakeDataSource.Returning(FakeDataSource.Offer("Noon", 5000m)));

        var result = await service.SearchAsync("ps5");

        Assert.Equal(2, result.OfferCount);
    }

    [Fact]
    public async Task SearchAsync_SkipsFailingSourceAndStillReturnsOthers()
    {
        var service = CreateService(
            FakeDataSource.Throwing(new InvalidOperationException("source down")),
            FakeDataSource.Returning(FakeDataSource.Offer("Amazon.sa", 4000m)));

        var result = await service.SearchAsync("ps5");

        Assert.Single(result.Offers);
        Assert.Equal("Amazon.sa", result.Offers[0].StoreName);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyResultWithoutRecommendation_WhenNoOffers()
    {
        var service = CreateService(FakeDataSource.Returning());

        var result = await service.SearchAsync("nonexistent");

        Assert.Empty(result.Offers);
        Assert.Null(result.Recommendation);
    }

    [Fact]
    public async Task SearchAsync_RealSourceExcludesFallbackOffers()
    {
        var service = CreateService(
            FakeDataSource.Returning(FakeDataSource.Offer("Noon", 2000m)),
            FakeDataSource.FallbackReturning(FakeDataSource.Offer("Mock Store", 100m)));

        var result = await service.SearchAsync("ps5");

        Assert.Single(result.Offers);
        Assert.Equal("Noon", result.Offers[0].StoreName);
        Assert.False(result.IsDemoData);
    }

    [Fact]
    public async Task SearchAsync_FallbackAlone_IsFlaggedAsDemoData()
    {
        var service = CreateService(
            FakeDataSource.Returning(),
            FakeDataSource.FallbackReturning(FakeDataSource.Offer("Mock Store", 100m)));

        var result = await service.SearchAsync("uncovered product");

        Assert.Single(result.Offers);
        Assert.True(result.IsDemoData);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_ReturnsEmptyResult_ForBlankQuery(string query)
    {
        var service = CreateService(FakeDataSource.Returning(FakeDataSource.Offer("Amazon.sa", 4000m)));

        var result = await service.SearchAsync(query);

        Assert.Empty(result.Offers);
        Assert.Equal(string.Empty, result.Query);
    }

    [Fact]
    public async Task SearchAsync_CheapSourceHasAMatch_ExpensiveLiveSourceIsStillCalledAndMerged()
    {
        // Curated-catalog products (iPhone 15, Galaxy S24, PS5, ...) must not
        // be capped at just their 2-3 manually-entered stores — live feeds
        // run alongside the catalog on every search, not only as a fallback,
        // so these products get the same store coverage as everything else.
        var cheap = FakeDataSource.Returning(FakeDataSource.Offer("Jarir", 2000m));
        var expensive = FakeDataSource.ExpensiveReturning(FakeDataSource.Offer("Amazon.sa", 1900m));
        var service = CreateService(cheap, expensive);

        var result = await service.SearchAsync("iphone 15");

        Assert.Equal(1, expensive.CallCount);
        Assert.Equal(2, result.Offers.Count);
        Assert.Equal("Amazon.sa", result.Offers[0].StoreName); // cheaper, ranked first
        Assert.Contains(result.Offers, o => o.StoreName == "Jarir");
    }

    [Fact]
    public async Task SearchAsync_NoCheapMatch_ExpensiveLiveSourceIsCalled()
    {
        var cheap = FakeDataSource.Returning(); // curated catalog miss
        var expensive = FakeDataSource.ExpensiveReturning(FakeDataSource.Offer("Amazon.sa", 1900m));
        var service = CreateService(cheap, expensive);

        var result = await service.SearchAsync("some obscure gadget");

        Assert.Equal(1, expensive.CallCount);
        Assert.Single(result.Offers);
        Assert.Equal("Amazon.sa", result.Offers[0].StoreName);
    }

    [Fact]
    public async Task SearchAsync_NoCheapMatch_FallbackDemoStillWorksAlongsideExpensiveSource()
    {
        var cheap = FakeDataSource.Returning();
        var expensive = FakeDataSource.ExpensiveReturning(); // live feed also found nothing
        var demo = FakeDataSource.FallbackReturning(FakeDataSource.Offer("Mock Store", 100m));
        var service = CreateService(cheap, expensive, demo);

        var result = await service.SearchAsync("totally uncovered thing");

        Assert.Equal(1, expensive.CallCount);
        Assert.Single(result.Offers);
        Assert.True(result.IsDemoData);
    }
}
