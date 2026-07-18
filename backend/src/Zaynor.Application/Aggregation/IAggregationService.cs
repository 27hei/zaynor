using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// The heart of Zaynor (spec Section 8): on each search it queries every data
/// source live, normalizes and merges the offers, ranks them by price, flags
/// the cheapest, and produces a recommendation.
/// </summary>
public interface IAggregationService
{
    Task<SearchResult> SearchAsync(string query, CancellationToken cancellationToken = default);
}
