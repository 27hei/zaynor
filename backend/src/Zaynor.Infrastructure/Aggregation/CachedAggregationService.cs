using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CachedAggregationService> _logger;

    /// <summary>
    /// The most recently dispatched background history-recording task, if any.
    /// Not used by production code (recording is intentionally fire-and-forget
    /// on the request path) — exists only so tests can deterministically await
    /// the background work instead of racing it.
    /// </summary>
    internal Task? LastBackgroundRecordingTask { get; private set; }

    public CachedAggregationService(
        AggregationService inner,
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        ILogger<CachedAggregationService> logger)
    {
        _inner = inner;
        _cache = cache;
        _scopeFactory = scopeFactory;
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
            // Fire-and-forget on its own DI scope/DbContext, not awaited on the
            // request path: this used to run synchronously here and, with up to
            // ~30 offers each needing 2 sequential unbatched DB round trips, was
            // adding several real seconds to every live search. Nothing in the
            // response depends on history having been written yet, so there's no
            // reason a slow DB should hold up the user waiting on results. Uses
            // its own scope (not the request's, which is disposed once the HTTP
            // response completes) and CancellationToken.None (a client
            // disconnecting shouldn't abort a write that's already in flight).
            RecordHistoryInBackground(result.Offers);

            // Only meaningful results are cached; failures/empties retry live.
            _cache.Set(cacheKey, result, CacheDuration);
        }

        return result;
    }

    private void RecordHistoryInBackground(IReadOnlyList<AggregatedOffer> offers)
    {
        LastBackgroundRecordingTask = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IPriceHistoryRecorder>();
                await recorder.RecordAsync(offers, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // RecordAsync already catches its own failures (spec NFR4); this
                // is a last-resort net around scope/DI resolution itself.
                _logger.LogWarning(ex, "Background price-history recording failed to start");
            }
        });
    }
}
