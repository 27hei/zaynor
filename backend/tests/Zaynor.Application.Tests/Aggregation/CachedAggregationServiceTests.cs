using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;
using Zaynor.Infrastructure.Aggregation;

namespace Zaynor.Application.Tests.Aggregation;

public class CachedAggregationServiceTests
{
    /// <summary>Counts recorder invocations so tests can assert history-write throttling.</summary>
    private sealed class CountingRecorder : IPriceHistoryRecorder
    {
        public int Calls { get; private set; }

        public Task RecordAsync(IReadOnlyList<AggregatedOffer> offers, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    /// <summary>Counts how often the inner engine actually runs (i.e., cache misses).</summary>
    private sealed class CountingDataSource : IProductDataSource
    {
        private readonly bool _returnsOffers;
        private readonly int _offerCount;

        public CountingDataSource(bool returnsOffers = true, int offerCount = 1)
        {
            _returnsOffers = returnsOffers;
            _offerCount = offerCount;
        }

        public int Calls { get; private set; }

        public string SourceName => "Counting";

        public Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            Calls++;
            IReadOnlyList<StoreOffer> offers = _returnsOffers
                ? Enumerable.Range(0, _offerCount).Select(i => FakeDataSource.Offer($"Store{i}", 100m + i)).ToList()
                : Array.Empty<StoreOffer>();
            return Task.FromResult(offers);
        }
    }

    private static (CachedAggregationService Service, CountingDataSource Source, CountingRecorder Recorder) CreateService(
        bool returnsOffers = true, int offerCount = 1)
    {
        var source = new CountingDataSource(returnsOffers, offerCount);
        var inner = new AggregationService([source], new AffiliateSettings(), new RankingWeights(), NullLogger<AggregationService>.Instance);
        var recorder = new CountingRecorder();
        var cache = new MemoryCache(new MemoryCacheOptions());

        // CachedAggregationService resolves IPriceHistoryRecorder from its own
        // DI scope (spec: fire-and-forget history writes), so the fake needs a
        // real scope factory to resolve from — registering the same recorder
        // instance keeps it observable to the test.
        var scopeFactory = new ServiceCollection()
            .AddScoped<IPriceHistoryRecorder>(_ => recorder)
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

        var service = new CachedAggregationService(
            inner, cache, scopeFactory, NullLogger<CachedAggregationService>.Instance);

        return (service, source, recorder);
    }

    [Fact]
    public async Task SearchAsync_SecondIdenticalQuery_IsServedFromCache()
    {
        var (service, source, recorder) = CreateService();

        var first = await service.SearchAsync("ps5");
        var second = await service.SearchAsync("ps5");

        // History recording is fire-and-forget (spec: doesn't block the
        // response), so wait for the dispatched background task before
        // asserting on its side effect instead of racing it.
        if (service.LastBackgroundRecordingTask is { } recordingTask)
        {
            await recordingTask;
        }

        Assert.Equal(1, source.Calls);
        Assert.Equal(1, recorder.Calls);
        // Pagination means the cache hit path always returns a freshly
        // sliced copy of the cached result (not the literal cached
        // instance), so reference equality no longer applies — source.Calls
        // staying at 1 is the real proof of a cache hit; this just confirms
        // the (re-sliced) content still matches.
        Assert.Equal(first.TotalCount, second.TotalCount);
        Assert.Equal(first.Recommendation, second.Recommendation);
        Assert.Equal(
            first.Offers.Select(o => (o.StoreName, o.Price)),
            second.Offers.Select(o => (o.StoreName, o.Price)));
    }

    [Fact]
    public async Task SearchAsync_CacheKeyIsNormalized_SoVariantsShareOneEntry()
    {
        var (service, source, _) = CreateService();

        await service.SearchAsync("Sony PlayStation 5");
        await service.SearchAsync("  sony   playstation 5! ");

        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task SearchAsync_DifferentQueries_AreFetchedSeparately()
    {
        var (service, source, _) = CreateService();

        await service.SearchAsync("ps5");
        await service.SearchAsync("iphone");

        Assert.Equal(2, source.Calls);
    }

    [Fact]
    public async Task SearchAsync_SlicesTheCachedResultByPage()
    {
        var (service, _, _) = CreateService(offerCount: 25);

        var page1 = await service.SearchAsync("widget", page: 1, pageSize: 10);
        var page2 = await service.SearchAsync("widget", page: 2, pageSize: 10);
        var page3 = await service.SearchAsync("widget", page: 3, pageSize: 10);

        Assert.Equal(10, page1.Offers.Count);
        Assert.Equal(10, page2.Offers.Count);
        Assert.Equal(5, page3.Offers.Count);
        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page2.Page);
        Assert.Equal(3, page3.Page);

        // No overlap between pages.
        var allStoreNames = page1.Offers.Concat(page2.Offers).Concat(page3.Offers).Select(o => o.StoreName).ToList();
        Assert.Equal(25, allStoreNames.Distinct().Count());
    }

    [Fact]
    public async Task SearchAsync_DifferentPagesOfSameQuery_ShareOneCacheEntry_NoSecondLiveFetch()
    {
        var (service, source, _) = CreateService(offerCount: 25);

        await service.SearchAsync("widget", page: 1, pageSize: 10);
        await service.SearchAsync("widget", page: 2, pageSize: 10);

        // Page 2 must not repeat the live vendor fan-out — every vendor is
        // only ever queried once per distinct query, regardless of how many
        // pages get requested.
        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task SearchAsync_PageSizeIsClampedToASaneMaximum()
    {
        var (service, _, _) = CreateService(offerCount: 25);

        var result = await service.SearchAsync("widget", page: 1, pageSize: 1000);

        Assert.True(result.PageSize <= 50);
    }

    [Fact]
    public async Task SearchAsync_EmptyResults_AreNotCachedOrRecorded()
    {
        var (service, source, recorder) = CreateService(returnsOffers: false);

        await service.SearchAsync("nothing");
        await service.SearchAsync("nothing");

        Assert.Equal(2, source.Calls);
        Assert.Equal(0, recorder.Calls);
    }
}
