using Microsoft.EntityFrameworkCore;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.Aggregation;

/// <summary>Reads recorded price points for a product, resolved by the FR3 normalized key.</summary>
public sealed class PriceHistoryService : IPriceHistoryService
{
    /// <summary>Upper bound on returned points to keep the payload small.</summary>
    private const int MaxPoints = 500;

    private readonly ZaynorDbContext _db;

    public PriceHistoryService(ZaynorDbContext db)
    {
        _db = db;
    }

    public async Task<PriceHistoryResponse> GetHistoryAsync(string query, CancellationToken cancellationToken = default)
    {
        var key = ProductNormalizer.Normalize(query);
        if (key.Length == 0)
        {
            return new PriceHistoryResponse { ProductName = null, Points = Array.Empty<PriceHistoryPoint>() };
        }

        var product = await _db.Products.FirstOrDefaultAsync(p => p.NormalizedKey == key, cancellationToken);
        if (product is null)
        {
            return new PriceHistoryResponse { ProductName = null, Points = Array.Empty<PriceHistoryPoint>() };
        }

        // Ordered by Id (monotonic = chronological) — SQLite cannot order by DateTimeOffset.
        var points = await _db.PriceHistory
            .Where(h => h.ProductId == product.Id)
            .OrderBy(h => h.Id)
            .Take(MaxPoints)
            .Join(_db.Stores, h => h.StoreId, s => s.Id, (h, s) => new PriceHistoryPoint
            {
                StoreName = s.Name,
                Price = h.Price,
                RecordedAt = h.RecordedAt,
            })
            .ToListAsync(cancellationToken);

        return new PriceHistoryResponse { ProductName = product.CanonicalName, Points = points };
    }
}
