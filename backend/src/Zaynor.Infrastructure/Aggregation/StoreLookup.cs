using Microsoft.EntityFrameworkCore;
using Zaynor.Domain.Entities;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.Aggregation;

/// <summary>
/// Single shared store-lookup-or-create, used by both <see cref="PriceHistoryRecorder"/>
/// and the review-submission path. <see cref="Store.Name"/> has no unique
/// index or normalization (a real latent gap: "Amazon.sa" typed by a
/// reviewer vs. "Amazon" recorded by the aggregator would previously create
/// two separate rows) — a case-insensitive/trimmed match here reduces that
/// risk without a new NormalizedKey column/migration, which the scale of the
/// actual problem doesn't justify.
/// </summary>
public static class StoreLookup
{
    public static Task<Store?> FindAsync(ZaynorDbContext db, string storeName, CancellationToken cancellationToken = default)
    {
        var trimmed = storeName.Trim();
        return db.Stores.FirstOrDefaultAsync(s => s.Name.ToLower() == trimmed.ToLower(), cancellationToken);
    }

    public static async Task<Store> FindOrCreateAsync(
        ZaynorDbContext db, string storeName, string? baseUrl, CancellationToken cancellationToken = default)
    {
        var existing = await FindAsync(db, storeName, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var store = new Store { Name = storeName.Trim(), BaseUrl = baseUrl ?? string.Empty };
        db.Stores.Add(store);
        await db.SaveChangesAsync(cancellationToken);
        return store;
    }
}
