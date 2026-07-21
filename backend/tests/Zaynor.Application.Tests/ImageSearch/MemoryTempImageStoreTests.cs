using Microsoft.Extensions.Caching.Memory;
using Zaynor.Infrastructure.ImageSearch;

namespace Zaynor.Application.Tests.ImageSearch;

public class MemoryTempImageStoreTests
{
    [Fact]
    public void SaveThenGet_RoundTripsTheBytesAndContentType()
    {
        var store = new MemoryTempImageStore(new MemoryCache(new MemoryCacheOptions()));
        var bytes = new byte[] { 1, 2, 3 };

        var id = store.Save(bytes, "image/png");
        var entry = store.Get(id);

        Assert.NotNull(entry);
        Assert.Equal(bytes, entry.Value.Bytes);
        Assert.Equal("image/png", entry.Value.ContentType);
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        var store = new MemoryTempImageStore(new MemoryCache(new MemoryCacheOptions()));

        Assert.Null(store.Get("never-saved"));
    }

    [Fact]
    public void Save_GeneratesADifferentIdEachTime()
    {
        var store = new MemoryTempImageStore(new MemoryCache(new MemoryCacheOptions()));

        var id1 = store.Save([1], "image/jpeg");
        var id2 = store.Save([2], "image/jpeg");

        Assert.NotEqual(id1, id2);
    }
}
