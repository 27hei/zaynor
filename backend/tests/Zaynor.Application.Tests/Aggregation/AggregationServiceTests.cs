using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Application.Aggregation;

namespace Zaynor.Application.Tests.Aggregation;

public class AggregationServiceTests
{
    private static AggregationService CreateService(params IProductDataSource[] sources) =>
        new(sources, NullLogger<AggregationService>.Instance);

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
}
