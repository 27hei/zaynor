using System.Text.Json;
using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Infrastructure.DataSources;

/// <summary>
/// The first REAL data source: a founder-maintained catalog of genuine market
/// prices (curated-catalog.json), the "manual entry" path the spec sanctions
/// for the pre-affiliate stage (Sections 9.4 and 20.7). Queries match products
/// via the FR3 normalizer over the name and per-product keywords, scored so the
/// most specific product wins ("s24 ultra" beats "s24"). Replaced/joined by
/// affiliate feeds later — the engine doesn't change.
/// </summary>
public sealed class CuratedProductDataSource : IProductDataSource
{
    private sealed record CatalogOffer(
        string Store, decimal Price, string Currency, string Url, bool FreeShipping, int? DeliveryDays);

    private sealed record CatalogProduct(string Name, List<string>? Keywords, List<CatalogOffer> Offers);

    private sealed record Catalog(string? LastUpdated, string? Note, List<CatalogProduct> Products);

    private sealed record IndexedProduct(CatalogProduct Product, string NameKey, List<string> KeywordKeys);

    private readonly List<IndexedProduct> _products = [];

    public CuratedProductDataSource(ILogger<CuratedProductDataSource> logger)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "curated-catalog.json");

        try
        {
            var json = File.ReadAllText(path);
            var catalog = JsonSerializer.Deserialize<Catalog>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            foreach (var product in catalog?.Products ?? [])
            {
                _products.Add(new IndexedProduct(
                    product,
                    ProductNormalizer.Normalize(product.Name),
                    (product.Keywords ?? []).Select(ProductNormalizer.Normalize).ToList()));
            }

            logger.LogInformation(
                "Curated catalog loaded: {Count} products (updated {Updated})",
                _products.Count, catalog?.LastUpdated ?? "unknown");
        }
        catch (Exception ex)
        {
            // A broken catalog must not take the engine down (NFR4).
            logger.LogError(ex, "Failed to load curated catalog from {Path}", path);
        }
    }

    public string SourceName => "CuratedCatalog";

    public Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var queryKey = ProductNormalizer.Normalize(query);
        if (queryKey.Length == 0 || _products.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<StoreOffer>>(Array.Empty<StoreOffer>());
        }

        var best = _products
            .Select(p => (Product: p, Score: Score(p, queryKey)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Product)
            .FirstOrDefault();

        if (best is null)
        {
            return Task.FromResult<IReadOnlyList<StoreOffer>>(Array.Empty<StoreOffer>());
        }

        IReadOnlyList<StoreOffer> offers = best.Product.Offers.Select(o => new StoreOffer
        {
            StoreName = o.Store,
            ProductTitle = best.Product.Name,
            Price = o.Price,
            Currency = o.Currency,
            ProductUrl = o.Url,
            InStock = true,
            FreeShipping = o.FreeShipping,
            DeliveryDays = o.DeliveryDays,
        }).ToList();

        return Task.FromResult(offers);
    }

    /// <summary>
    /// Match score: exact key beats query-contains-key beats key-contains-query,
    /// longer keys beating shorter — so the most specific product wins.
    /// </summary>
    private static int Score(IndexedProduct product, string queryKey)
    {
        var keys = product.KeywordKeys.Append(product.NameKey);
        var best = 0;

        foreach (var key in keys)
        {
            if (key.Length == 0) continue;

            var score = key == queryKey
                ? 1000 + key.Length
                : queryKey.Contains(key)
                    ? 500 + key.Length
                    : key.Contains(queryKey)
                        ? queryKey.Length
                        : 0;

            best = Math.Max(best, score);
        }

        return best;
    }
}
