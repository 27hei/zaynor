using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.DataSources;

/// <summary>
/// A temporary, in-memory data source that fabricates plausible offers so the
/// full search → aggregate → rank → recommend flow works end-to-end before any
/// real feed is wired (spec Section 23, step 5; Section 9.4). Prices are derived
/// deterministically from the query so results are stable between calls.
///
/// This will be replaced/supplemented by real sources (affiliate feeds, store
/// APIs) — the aggregation engine does not change when that happens, it just
/// gains more <see cref="IProductDataSource"/> registrations.
/// </summary>
public sealed class MockProductDataSource : IProductDataSource
{
    private const string Currency = "SAR";

    // A small set of stores relevant to the Saudi market (spec Section 6, 20.3),
    // with plausible shipping profiles so result rows can show shipping info
    // (spec Section 16: per-store row shows price, stock, and shipping).
    private static readonly StoreProfile[] Stores =
    [
        new("Amazon.sa", "https://www.amazon.sa", 1.00m, FreeShipping: true, DeliveryDays: 1),
        new("Noon", "https://www.noon.com", 1.08m, FreeShipping: true, DeliveryDays: 2),
        new("Jarir", "https://www.jarir.com", 1.05m, FreeShipping: true, DeliveryDays: 3),
        new("Extra", "https://www.extra.com", 1.12m, FreeShipping: false, DeliveryDays: 4),
        new("AliExpress", "https://www.aliexpress.com", 0.94m, FreeShipping: false, DeliveryDays: 9),
    ];

    public string SourceName => "MockDataSource";

    public Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var trimmed = query.Trim();
        if (trimmed.Length == 0)
        {
            return Task.FromResult<IReadOnlyList<StoreOffer>>(Array.Empty<StoreOffer>());
        }

        var basePrice = DeriveBasePrice(trimmed);

        var offers = Stores.Select(store => new StoreOffer
        {
            StoreName = store.Name,
            ProductTitle = trimmed,
            Price = Math.Round(basePrice * store.PriceFactor, 2),
            Currency = Currency,
            ProductUrl = BuildAffiliateUrl(store, trimmed),
            InStock = true,
            FreeShipping = store.FreeShipping,
            DeliveryDays = store.DeliveryDays,
        }).ToList();

        return Task.FromResult<IReadOnlyList<StoreOffer>>(offers);
    }

    /// <summary>
    /// Maps a query to a stable base price in a realistic range (roughly
    /// 200–5,200 SAR) so different searches return different, repeatable prices.
    /// </summary>
    private static decimal DeriveBasePrice(string query)
    {
        var hash = 0;
        foreach (var ch in query.ToLowerInvariant())
        {
            hash = unchecked((hash * 31) + ch);
        }

        var positive = Math.Abs(hash);
        return 200m + (positive % 5000);
    }

    private static string BuildAffiliateUrl(StoreProfile store, string query)
    {
        var slug = Uri.EscapeDataString(query);
        // The "aff=zaynor" marker stands in for the real affiliate tracking
        // parameter that a network link will carry (spec FR7, Section 10).
        return $"{store.BaseUrl}/search?q={slug}&aff=zaynor";
    }

    private sealed record StoreProfile(
        string Name,
        string BaseUrl,
        decimal PriceFactor,
        bool FreeShipping,
        int DeliveryDays);
}
