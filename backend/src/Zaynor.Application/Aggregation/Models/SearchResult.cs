namespace Zaynor.Application.Aggregation.Models;

/// <summary>
/// The full outcome of a search: the aggregated offers (sorted cheapest first)
/// and the recommendation. This is the read model returned to the API.
/// </summary>
public sealed record SearchResult
{
    public required string Query { get; init; }

    /// <summary>Offers sorted by price ascending (spec FR4).</summary>
    public required IReadOnlyList<AggregatedOffer> Offers { get; init; }

    /// <summary>The recommendation, or null when no offers were found.</summary>
    public Recommendation? Recommendation { get; init; }

    /// <summary>
    /// True when the offers came only from a fallback (demo) source — the UI
    /// labels these honestly instead of presenting them as market prices.
    /// </summary>
    public bool IsDemoData { get; init; }

    public int OfferCount => Offers.Count;
}
