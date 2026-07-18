namespace Zaynor.Application.Aggregation;

/// <summary>
/// Search autocomplete (competitive analysis Section 2, table stakes #1).
/// Suggestions come from products Zaynor has actually seen — the Products
/// table that accumulates as searches run — so they are honest and grow
/// richer as usage grows.
/// </summary>
public interface ISearchSuggestionService
{
    Task<IReadOnlyList<string>> GetSuggestionsAsync(string prefix, int limit, CancellationToken cancellationToken = default);
}
