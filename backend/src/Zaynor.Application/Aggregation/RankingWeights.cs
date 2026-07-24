namespace Zaynor.Application.Aggregation;

/// <summary>
/// How much each factor in <see cref="OfferScorer"/> contributes to an
/// offer's final rank score. Bound once from config (Ranking:Weights:*) —
/// same pattern as <see cref="AffiliateSettings"/> — so the founder can
/// retune ranking without a redeploy. Defaults are a reasonable starting
/// point, not a scientifically derived optimum, and sum to 1.0.
/// </summary>
public sealed class RankingWeights
{
    public double TitleMatch { get; init; } = 0.30;

    public double Price { get; init; } = 0.25;

    public double Rating { get; init; } = 0.15;

    public double ReviewCount { get; init; } = 0.10;

    public double Confidence { get; init; } = 0.10;

    public double Availability { get; init; } = 0.05;

    public double Freshness { get; init; } = 0.05;
}
