using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// A source of product offers (an affiliate feed, a store API, or — later — a
/// scraper). The aggregation engine fans a search out across every registered
/// source and merges the results. New sources are added by implementing this
/// interface and registering it; no change to the aggregation engine is needed.
/// See spec Section 9 (Data Acquisition) and Section 13 (Architecture).
/// </summary>
public interface IProductDataSource
{
    /// <summary>A short identifier for the source, used in logging/diagnostics.</summary>
    string SourceName { get; }

    /// <summary>
    /// Fallback sources (demo/mock data) only contribute when no real source
    /// covered the query; their results are flagged as demo data to the user.
    /// </summary>
    bool IsFallback => false;

    /// <summary>
    /// Marks a quota-limited live feed (paid trial credits, rate-limited
    /// APIs) for logging/diagnostics purposes only — it no longer changes
    /// whether the source is queried. It used to skip these whenever the
    /// curated catalog already had a match, but that silently capped the
    /// handful of curated products at just 2-3 stores instead of the dozens
    /// live search finds for everything else (spec: founder's call —
    /// maximum store coverage per search matters more than quota
    /// conservation), so every source now runs on every search.
    /// </summary>
    bool IsExpensiveLive => false;

    /// <summary>
    /// How much to trust this source's offers relative to others, from 0 to 1,
    /// used as one factor in multi-factor ranking (see OfferScorer). A rough,
    /// deliberately coarse tier — not a measured/learned trust score — so it
    /// stays honest about how little real signal backs it today. Defaults to
    /// full trust; override only with a stated reason.
    /// </summary>
    double Confidence => 1.0;

    /// <summary>
    /// Returns the offers this source has for the given query. Implementations
    /// should fail soft (return an empty list rather than throw) so one bad
    /// source does not break the whole search (spec NFR4).
    /// </summary>
    Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
