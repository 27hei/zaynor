using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Tests.Aggregation;

public class OfferScorerTests
{
    private static readonly RankingWeights Weights = new();

    private static StoreOffer Offer(
        string store = "Store",
        string title = "Test Product",
        decimal price = 100m,
        bool inStock = true,
        decimal? rating = null,
        int? ratingCount = null) => new()
    {
        StoreName = store,
        ProductTitle = title,
        Price = price,
        Currency = "SAR",
        ProductUrl = $"https://example.com/{store}",
        InStock = inStock,
        Rating = rating,
        RatingCount = ratingCount,
    };

    [Fact]
    public void ScoreAll_ReturnsEmpty_ForEmptyInput()
    {
        var scores = OfferScorer.ScoreAll([], [], "test", Weights);

        Assert.Empty(scores);
    }

    [Fact]
    public void ScoreAll_CheaperOffer_ScoresHigher_AllElseEqual()
    {
        var offers = new[] { Offer(price: 100m), Offer(price: 50m) };
        var scores = OfferScorer.ScoreAll(offers, [1.0, 1.0], "test product", Weights);

        Assert.True(scores[1] > scores[0]);
    }

    [Fact]
    public void ScoreAll_HigherRating_ScoresHigher_AllElseEqual()
    {
        var offers = new[] { Offer(rating: 3.0m), Offer(rating: 5.0m) };
        var scores = OfferScorer.ScoreAll(offers, [1.0, 1.0], "test product", Weights);

        Assert.True(scores[1] > scores[0]);
    }

    [Fact]
    public void ScoreAll_UnratedOffer_ScoresBetweenLowAndHighRated()
    {
        // Neutral (0.6), not punished to 0 just for lacking a rating.
        var offers = new[] { Offer(rating: 1.0m), Offer(rating: null), Offer(rating: 5.0m) };
        var scores = OfferScorer.ScoreAll(offers, [1.0, 1.0, 1.0], "test product", Weights);

        Assert.True(scores[0] < scores[1]);
        Assert.True(scores[1] < scores[2]);
    }

    [Fact]
    public void ScoreAll_HigherReviewCount_ScoresHigher_AllElseEqual()
    {
        var offers = new[]
        {
            Offer(rating: 4.5m, ratingCount: 5),
            Offer(rating: 4.5m, ratingCount: 5000),
        };
        var scores = OfferScorer.ScoreAll(offers, [1.0, 1.0], "test product", Weights);

        Assert.True(scores[1] > scores[0]);
    }

    [Fact]
    public void ScoreAll_HigherProviderConfidence_ScoresHigher_AllElseEqual()
    {
        var offers = new[] { Offer(), Offer() };
        var scores = OfferScorer.ScoreAll(offers, [0.7, 1.0], "test product", Weights);

        Assert.True(scores[1] > scores[0]);
    }

    [Fact]
    public void ScoreAll_InStockOffer_ScoresHigherThanOutOfStock_AllElseEqual()
    {
        var offers = new[] { Offer(inStock: false), Offer(inStock: true) };
        var scores = OfferScorer.ScoreAll(offers, [1.0, 1.0], "test product", Weights);

        Assert.True(scores[1] > scores[0]);
    }

    [Fact]
    public void ScoreAll_CloserTitleMatch_ScoresHigher_AllElseEqual()
    {
        var offers = new[]
        {
            Offer(title: "Some Unrelated Accessory Bundle"),
            Offer(title: "Samsung Galaxy Watch 7"),
        };
        var scores = OfferScorer.ScoreAll(offers, [1.0, 1.0], "samsung galaxy watch 7", Weights);

        Assert.True(scores[1] > scores[0]);
    }

    [Fact]
    public void ScoreAll_AllSamePrice_EveryoneScoresFullMarksOnPriceFactor()
    {
        // Regression guard for the division-by-zero shape (max == min).
        var offers = new[] { Offer(price: 100m), Offer(price: 100m) };
        var scores = OfferScorer.ScoreAll(offers, [1.0, 1.0], "test product", Weights);

        Assert.Equal(scores[0], scores[1], precision: 10);
    }

    [Fact]
    public void ScoreAll_MultiFactor_CanRankAPricierButBetterOfferAboveTheCheapest()
    {
        // The whole point of multi-factor ranking: cheapest isn't always
        // best. A third, distinctly-pricier offer widens the price range so
        // "B"'s small (5%) premium over the cheapest isn't scored as a full
        // 0-vs-1 price swing (with only two offers, price score is always
        // binary regardless of how close the actual prices are) — with a
        // realistic gap, a genuinely popular, well-rated offer should
        // outrank the cheapest, unrated one.
        var cheapUnrated = Offer(store: "CheapStore", price: 100m, rating: null, ratingCount: null);
        var slightlyPricierWellRated = Offer(store: "GoodStore", price: 105m, rating: 5.0m, ratingCount: 5000);
        var mostExpensiveUnrated = Offer(store: "PricyStore", price: 200m, rating: null, ratingCount: null);

        var scores = OfferScorer.ScoreAll(
            [cheapUnrated, slightlyPricierWellRated, mostExpensiveUnrated],
            [1.0, 1.0, 1.0],
            "test product",
            Weights);

        Assert.True(scores[1] > scores[0]);
    }

    [Fact]
    public void ScoreAll_EveryScore_StaysWithinZeroToOne()
    {
        var offers = new[]
        {
            Offer(price: 1m, inStock: false, rating: 1.0m, ratingCount: 1),
            Offer(price: 100000m, inStock: true, rating: 5.0m, ratingCount: 999999),
        };
        var scores = OfferScorer.ScoreAll(offers, [0.0, 1.0], "test product", Weights);

        Assert.All(scores, s => Assert.InRange(s, 0.0, 1.0));
    }
}
