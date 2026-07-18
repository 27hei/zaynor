using Microsoft.EntityFrameworkCore;
using Zaynor.Application.Aggregation;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.Aggregation;

/// <summary>
/// Suggests canonical product names whose normalized key contains the
/// normalized input — matching is diacritic/case/punctuation-insensitive via
/// the same FR3 normalizer the aggregation engine uses.
/// </summary>
public sealed class SearchSuggestionService : ISearchSuggestionService
{
    private readonly ZaynorDbContext _db;

    public SearchSuggestionService(ZaynorDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> GetSuggestionsAsync(string prefix, int limit, CancellationToken cancellationToken = default)
    {
        var key = ProductNormalizer.Normalize(prefix);
        if (key.Length < 2)
        {
            return Array.Empty<string>();
        }

        return await _db.Products
            .Where(p => p.NormalizedKey.Contains(key))
            .OrderBy(p => p.CanonicalName)
            .Select(p => p.CanonicalName)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
