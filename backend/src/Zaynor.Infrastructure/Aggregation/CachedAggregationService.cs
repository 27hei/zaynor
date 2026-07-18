using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.Aggregation;

/// <summary>
/// Decorates the core aggregation engine with a short-lived cache (spec
/// Section 13: feeds refresh every few hours, so brief caching keeps repeat
/// searches instant without staleness) and records price history on live
/// fetches only — cache hits observe nothing new, so the cache naturally
/// throttles history writes too.
/// </summary>
public sealed class CachedAggregationService : IAggregationService
{
    /// <summary>How long a search result stays fresh before re-fetching live.</summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly AggregationService _inner;
    private readonly IMemoryCache _cache;
    private readonly IPriceHistoryRecorder _historyRecorder;
    private readonly ILogger<CachedAggregationService> _logger;

    public CachedAggregationService(
        AggregationService inner,
        IMemoryCache cache,
        IPriceHistoryRecorder historyRecorder,
        ILogger<CachedAggregationService> logger)
    {
        _inner = inner;
        _cache = cache;
        _historyRecorder = historyRecorder;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"search:{ProductNormalizer.Normalize(query ?? string.Empty)}";

        if (_cache.TryGetValue<SearchResult>(cacheKey, out var cached) && cached is not null)
        {
            _logger.LogDebug("Cache hit for {Query}", query);
            return cached;
        }

        var result = await _inner.SearchAsync(query!, cancellationToken);

        if (result.Offers.Count > 0)
        {
            // Live observation: accumulate history (spec Sections 13/15, feeds FR12).
            await _historyRecorder.RecordAsync(result.Offers, cancellationToken);

            // Only meaningful results are cached; failures/empties retry live.
            _cache.Set(cacheKey, result, CacheDuration);
        }

        return result;
    }
}
