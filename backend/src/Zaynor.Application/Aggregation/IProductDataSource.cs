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
    /// Returns the offers this source has for the given query. Implementations
    /// should fail soft (return an empty list rather than throw) so one bad
    /// source does not break the whole search (spec NFR4).
    /// </summary>
    Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
