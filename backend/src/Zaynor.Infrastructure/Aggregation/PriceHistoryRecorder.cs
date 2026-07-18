using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;
using Zaynor.Domain.Entities;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.Aggregation;

/// <summary>
/// Persists observed prices into PriceHistory after a live search (spec
/// Sections 13/15), finding-or-creating the Product (via the FR3 normalized
/// key) and each Store on first sight. A per-store throttle avoids duplicate
/// rows when the same product is searched repeatedly.
/// </summary>
public sealed class PriceHistoryRecorder : IPriceHistoryRecorder
{
    /// <summary>Minimum gap between recorded points for the same product+store.</summary>
    private static readonly TimeSpan RecordInterval = TimeSpan.FromHours(1);

    private readonly ZaynorDbContext _db;
    private readonly ILogger<PriceHistoryRecorder> _logger;

    public PriceHistoryRecorder(ZaynorDbContext db, ILogger<PriceHistoryRecorder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RecordAsync(IReadOnlyList<AggregatedOffer> offers, CancellationToken cancellationToken = default)
    {
        if (offers.Count == 0)
        {
            return;
        }

        // History accumulation must never break the search itself (NFR4).
        try
        {
            var product = await FindOrCreateProductAsync(offers[0], cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var added = 0;

            foreach (var offer in offers)
            {
                var store = await FindOrCreateStoreAsync(offer, cancellationToken);

                if (await RecordedRecentlyAsync(product.Id, store.Id, now, cancellationToken))
                {
                    continue;
                }

                _db.PriceHistory.Add(new PriceHistory
                {
                    ProductId = product.Id,
                    StoreId = store.Id,
                    Price = offer.Price,
                    RecordedAt = now,
                });
                added++;
            }

            if (added > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Recorded {Count} price points for product {Product}", added, product.CanonicalName);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Price-history recording failed; the search result is unaffected");
        }
    }

    private async Task<Product> FindOrCreateProductAsync(AggregatedOffer offer, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.NormalizedKey == offer.NormalizedKey, cancellationToken);

        if (product is not null)
        {
            return product;
        }

        product = new Product { CanonicalName = offer.ProductTitle, NormalizedKey = offer.NormalizedKey };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return product;
    }

    private async Task<Store> FindOrCreateStoreAsync(AggregatedOffer offer, CancellationToken cancellationToken)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Name == offer.StoreName, cancellationToken);
        if (store is not null)
        {
            return store;
        }

        store = new Store
        {
            Name = offer.StoreName,
            BaseUrl = SafeOrigin(offer.ProductUrl),
        };
        _db.Stores.Add(store);
        await _db.SaveChangesAsync(cancellationToken);
        return store;
    }

    /// <summary>
    /// True when a point for this product+store was recorded within
    /// <see cref="RecordInterval"/>. The latest row is found by monotonic Id
    /// and compared in memory — SQLite cannot compare DateTimeOffset in SQL.
    /// </summary>
    private async Task<bool> RecordedRecentlyAsync(int productId, int storeId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var latest = await _db.PriceHistory
            .Where(h => h.ProductId == productId && h.StoreId == storeId)
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return latest is not null && now - latest.RecordedAt < RecordInterval;
    }

    private static string SafeOrigin(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Authority)
            : url;
    }
}
