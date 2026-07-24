namespace Zaynor.Application.Aggregation.Models;

/// <summary>
/// The full outcome of a search: the aggregated offers (sorted cheapest first)
/// and the recommendation. This is the read model returned to the API.
/// </summary>
public sealed record SearchResult
{
    public required string Query { get; init; }

    /// <summary>
    /// Set only when a known colloquial Arabic brand spelling was corrected
    /// before searching (e.g. "سامسنج" → "Samsung") — surfaced so the UI can
    /// tell the visitor what was actually searched for, the same way Google
    /// itself shows "Showing results for X" after a silent correction.
    /// </summary>
    public string? CorrectedQuery { get; init; }

    /// <summary>
    /// This page's offers, ranked by multi-factor score (see OfferScorer) —
    /// no longer simply price ascending; the cheapest is still flagged via
    /// IsLowestPrice regardless of rank order.
    /// </summary>
    public required IReadOnlyList<AggregatedOffer> Offers { get; init; }

    /// <summary>The recommendation, or null when no offers were found.</summary>
    public Recommendation? Recommendation { get; init; }

    /// <summary>
    /// True when the offers came only from a fallback (demo) source — the UI
    /// labels these honestly instead of presenting them as market prices.
    /// </summary>
    public bool IsDemoData { get; init; }

    /// <summary>Offers on THIS page only — see <see cref="TotalCount"/> for the full result size.</summary>
    public int OfferCount => Offers.Count;

    /// <summary>1-based page number returned (see CachedAggregationService — every vendor is only ever called once per search; pages are sliced from that one merged/ranked result).</summary>
    public int Page { get; init; } = 1;

    public int PageSize { get; init; }

    /// <summary>Total offers across every page, before slicing.</summary>
    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}
