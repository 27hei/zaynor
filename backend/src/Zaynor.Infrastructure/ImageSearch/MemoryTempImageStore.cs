using Microsoft.Extensions.Caching.Memory;
using Zaynor.Application.ImageSearch;

namespace Zaynor.Infrastructure.ImageSearch;

/// <summary>
/// In-process, in-memory temp storage for uploaded photos (see
/// <see cref="ITempImageStore"/>). Fine for a single-instance deployment;
/// entries expire after a few minutes regardless of whether they were read,
/// so nothing lingers even if the reverse-image lookup never fetches them.
/// </summary>
public sealed class MemoryTempImageStore : ITempImageStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    public MemoryTempImageStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string Save(byte[] bytes, string contentType)
    {
        var id = Guid.NewGuid().ToString("N");
        _cache.Set(CacheKey(id), (bytes, contentType), Ttl);
        return id;
    }

    public (byte[] Bytes, string ContentType)? Get(string id) =>
        _cache.TryGetValue(CacheKey(id), out (byte[] Bytes, string ContentType) entry) ? entry : null;

    private static string CacheKey(string id) => $"temp-image:{id}";
}
