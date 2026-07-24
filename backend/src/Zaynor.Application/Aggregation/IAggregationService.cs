using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// The heart of Zaynor (spec Section 8): on each search it queries every data
/// source live, normalizes and merges the offers, ranks them by price, flags
/// the cheapest, and produces a recommendation.
/// </summary>
public interface IAggregationService
{
    /// <summary>
    /// Searches every configured source, merges/ranks the results, and
    /// returns just <paramref name="page"/> (1-based) sliced to
    /// <paramref name="pageSize"/> offers — vendors are only ever queried
    /// once per distinct query regardless of how many pages get requested
    /// (see CachedAggregationService, which caches the full merged result
    /// and slices per call).
    /// </summary>
    Task<SearchResult> SearchAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
