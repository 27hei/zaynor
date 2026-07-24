using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// Computes a 0-1 multi-factor rank score per offer, replacing plain
/// cheapest-first ordering. Each factor is normalized WITHIN the current
/// result set (e.g. the cheapest offer in this batch scores 1.0 on price,
/// not "cheap on some absolute scale") — a search only ever ranks its own
/// results against each other.
/// </summary>
public static class OfferScorer
{
    /// <summary>
    /// Returns one score per offer, same order/index as <paramref name="offers"/>
    /// and <paramref name="confidences"/> (parallel arrays — avoids using
    /// StoreOffer, a record with structural equality, as a dictionary key,
    /// where two coincidentally-identical offers could collide).
    /// </summary>
    public static IReadOnlyList<double> ScoreAll(
        IReadOnlyList<StoreOffer> offers,
        IReadOnlyList<double> confidences,
        string normalizedQuery,
        RankingWeights weights)
    {
        if (offers.Count == 0)
        {
            return [];
        }

        var minPrice = offers.Min(o => o.Price);
        var maxPrice = offers.Max(o => o.Price);
        var maxRatingCount = offers.Max(o => o.RatingCount ?? 0);

        var scores = new List<double>(offers.Count);
        for (var i = 0; i < offers.Count; i++)
        {
            var offer = offers[i];

            var titleMatch = TitleRelevance.NormalizedScore(
                normalizedQuery, [ProductNormalizer.Normalize(offer.ProductTitle)]);

            // Cheapest in this batch = 1.0, most expensive = 0.0. If every
            // offer is the same price, nobody is penalized (all score 1.0).
            var priceScore = maxPrice == minPrice
                ? 1.0
                : 1.0 - (double)((offer.Price - minPrice) / (maxPrice - minPrice));

            // 0.6 (neutral, not punitive) when unrated — an unrated offer
            // isn't necessarily a bad one, just an unknown.
            var ratingScore = offer.Rating is { } r ? Math.Clamp((double)r / 5.0, 0, 1) : 0.6;

            var reviewScore = maxRatingCount > 0 && offer.RatingCount is { } rc
                ? Math.Log10(rc + 1) / Math.Log10(maxRatingCount + 1)
                : 0.0;

            var confidenceScore = Math.Clamp(confidences[i], 0, 1);
            var availabilityScore = offer.InStock ? 1.0 : 0.0;

            // Always 1.0 today: every offer in a batch is scored once, at
            // live-fetch time, before any caching happens — so within a
            // single batch this contributes the same constant to everyone
            // and never changes relative order. No vendor exposes a genuine
            // per-listing freshness signal today; this factor is a real,
            // intentional hook for one later (or for cache-aware re-ranking
            // on a stale cache hit) rather than fabricated precision now.
            var freshnessScore = 1.0;

            var score =
                weights.TitleMatch * titleMatch +
                weights.Price * priceScore +
                weights.Rating * ratingScore +
                weights.ReviewCount * reviewScore +
                weights.Confidence * confidenceScore +
                weights.Availability * availabilityScore +
                weights.Freshness * freshnessScore;

            scores.Add(Math.Clamp(score, 0.0, 1.0));
        }

        return scores;
    }
}
