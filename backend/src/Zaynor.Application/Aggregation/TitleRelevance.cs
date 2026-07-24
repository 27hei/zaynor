namespace Zaynor.Application.Aggregation;

/// <summary>
/// Scores how well a normalized query matches a set of normalized title/
/// keyword keys. Originally lived only inside CuratedProductDataSource (to
/// pick which single catalog product answers a query); extracted so
/// OfferScorer can reuse the exact same tiered logic as one factor in
/// multi-listing ranking, instead of duplicating it.
/// </summary>
public static class TitleRelevance
{
    /// <summary>
    /// Tiered match score: exact key beats query-contains-key beats
    /// key-contains-query, longer keys beating shorter within a tier — so the
    /// most specific match wins. Falls back to a whole-query word-overlap
    /// check when no single key matches by containment. Inputs should
    /// already be <see cref="ProductNormalizer"/>.Normalize'd.
    /// </summary>
    public static int Score(string normalizedQuery, IEnumerable<string> normalizedKeys)
    {
        var keys = normalizedKeys.Where(k => k.Length > 0).ToList();
        var best = 0;

        foreach (var key in keys)
        {
            var score = key == normalizedQuery
                ? 10000 + key.Length
                : normalizedQuery.Contains(key)
                    ? 5000 + key.Length
                    : key.Contains(normalizedQuery)
                        ? 1000 + normalizedQuery.Length
                        : 0;

            best = Math.Max(best, score);
        }

        if (best > 0)
        {
            return best;
        }

        return WordOverlapScore(keys, normalizedQuery);
    }

    /// <summary>
    /// <see cref="Score"/> scaled to 0-1 for use as a ranking factor — 10000
    /// (an exact match) is treated as "perfect", everything below scaled
    /// linearly against it.
    /// </summary>
    public static double NormalizedScore(string normalizedQuery, IEnumerable<string> normalizedKeys) =>
        Math.Min(1.0, Score(normalizedQuery, normalizedKeys) / 10000.0);

    /// <summary>
    /// Fallback match for queries phrased differently than any single stored
    /// key — e.g. "samsung 55 tv" won't appear verbatim in "Samsung 55" Crystal
    /// UHD DU7000 4K Smart TV", but every one of its words does, just spread
    /// across the name and keywords. A query only matches this way when ALL
    /// of its words are found somewhere in the combined vocabulary, so
    /// unrelated items (sharing at most one stray word) don't get pulled in
    /// as false positives.
    /// </summary>
    private static int WordOverlapScore(IReadOnlyList<string> keys, string queryKey)
    {
        var queryWords = queryKey.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (queryWords.Length < 2)
        {
            return 0;
        }

        var vocabulary = new HashSet<string>(
            keys.SelectMany(k => k.Split(' ', StringSplitOptions.RemoveEmptyEntries)));

        var matched = queryWords.Count(vocabulary.Contains);
        return matched == queryWords.Length ? 200 + matched : 0;
    }
}
