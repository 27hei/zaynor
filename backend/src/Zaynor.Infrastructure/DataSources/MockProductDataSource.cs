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
        new("Amazon.sa", "https://www.amazon.sa/s?k={0}", 1.00m, FreeShipping: true, DeliveryDays: 1),
        new("Noon", "https://www.noon.com/saudi-en/search/?q={0}", 1.08m, FreeShipping: true, DeliveryDays: 2),
        new("Jarir", "https://www.jarir.com/sa-en/catalogsearch/result/?search={0}", 1.05m, FreeShipping: true, DeliveryDays: 3),
        new("Extra", "https://www.extra.com/en-sa/search?q={0}", 1.12m, FreeShipping: false, DeliveryDays: 4),
        new("AliExpress", "https://www.aliexpress.com/wholesale?SearchText={0}", 0.94m, FreeShipping: false, DeliveryDays: 9),
    ];

    public string SourceName => "MockDataSource";

    /// <summary>Demo data: used only when no real source covers the query.</summary>
    public bool IsFallback => true;

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
            ProductUrl = BuildSearchUrl(store, trimmed),
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

    private static string BuildSearchUrl(StoreProfile store, string query)
    {
        var slug = Uri.EscapeDataString(query);
        // Each store's real search results page (spec FR7, Section 10) — not a
        // fabricated product link, since there's no real offer to point to yet.
        // /api/out appends the real affiliate tag for stores we're live on
        // (e.g. Amazon.sa's zaynor-21) on top of this.
        return string.Format(store.SearchUrlTemplate, slug);
    }

    private sealed record StoreProfile(
        string Name,
        string SearchUrlTemplate,
        decimal PriceFactor,
        bool FreeShipping,
        int DeliveryDays);
}
