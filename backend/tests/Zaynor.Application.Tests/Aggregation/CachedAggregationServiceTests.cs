using Microsoft.Extensions.Caching.Memory;
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

        public CountingDataSource(bool returnsOffers = true)
        {
            _returnsOffers = returnsOffers;
        }

        public int Calls { get; private set; }

        public string SourceName => "Counting";

        public Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            Calls++;
            IReadOnlyList<StoreOffer> offers = _returnsOffers
                ? [FakeDataSource.Offer("Amazon.sa", 100m)]
                : Array.Empty<StoreOffer>();
            return Task.FromResult(offers);
        }
    }

    private static (CachedAggregationService Service, CountingDataSource Source, CountingRecorder Recorder) CreateService(
        bool returnsOffers = true)
    {
        var source = new CountingDataSource(returnsOffers);
        var inner = new AggregationService([source], new AffiliateSettings(), NullLogger<AggregationService>.Instance);
        var recorder = new CountingRecorder();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new CachedAggregationService(
            inner, cache, recorder, NullLogger<CachedAggregationService>.Instance);

        return (service, source, recorder);
    }

    [Fact]
    public async Task SearchAsync_SecondIdenticalQuery_IsServedFromCache()
    {
        var (service, source, recorder) = CreateService();

        var first = await service.SearchAsync("ps5");
        var second = await service.SearchAsync("ps5");

        Assert.Equal(1, source.Calls);
        Assert.Equal(1, recorder.Calls);
        Assert.Same(first, second);
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
    public async Task SearchAsync_EmptyResults_AreNotCachedOrRecorded()
    {
        var (service, source, recorder) = CreateService(returnsOffers: false);

        await service.SearchAsync("nothing");
        await service.SearchAsync("nothing");

        Assert.Equal(2, source.Calls);
        Assert.Equal(0, recorder.Calls);
    }
}
