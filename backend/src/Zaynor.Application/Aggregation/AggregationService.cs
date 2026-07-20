using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// Default aggregation engine. Fans a search out across every registered
/// <see cref="IProductDataSource"/> concurrently, merges the offers, sorts them
/// cheapest-first, flags the lowest, and builds a recommendation. Stateless per
/// request (spec Section 13).
/// </summary>
public sealed class AggregationService : IAggregationService
{
    private readonly IReadOnlyList<IProductDataSource> _sources;
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(
        IEnumerable<IProductDataSource> sources,
        ILogger<AggregationService> logger)
    {
        _sources = sources.ToList();
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return new SearchResult { Query = string.Empty, Offers = Array.Empty<AggregatedOffer>() };
        }

        var (realOffers, fallbackOffers) = await GatherOffersAsync(trimmed, cancellationToken);

        // Real sources win outright; fallback (demo) offers appear only when
        // nothing real covered the query, and are flagged as demo data.
        var isDemo = realOffers.Count == 0 && fallbackOffers.Count > 0;
        var offers = realOffers.Count > 0 ? realOffers : fallbackOffers;

        if (offers.Count == 0)
        {
            _logger.LogInformation("No offers found for query {Query}", trimmed);
            return new SearchResult { Query = trimmed, Offers = Array.Empty<AggregatedOffer>() };
        }

        var ranked = RankOffers(offers);
        var recommendation = BuildRecommendation(ranked);

        return new SearchResult
        {
            Query = trimmed,
            Offers = ranked,
            Recommendation = recommendation,
            IsDemoData = isDemo,
        };
    }

    /// <summary>
    /// Queries cheap sources (the curated catalog) first; quota-limited live
    /// feeds only fire when nothing cheap matched, which both conserves their
    /// request budgets and keeps a verified curated match from being diluted
    /// by unrelated live search results. A source that throws is logged and
    /// skipped so one failing source never breaks the whole search (spec NFR4).
    /// </summary>
    private async Task<(List<StoreOffer> Real, List<StoreOffer> Fallback)> GatherOffersAsync(
        string query, CancellationToken cancellationToken)
    {
        var cheapSources = _sources.Where(s => !s.IsExpensiveLive).ToList();
        var expensiveSources = _sources.Where(s => s.IsExpensiveLive).ToList();

        var cheapResults = await QueryAllAsync(cheapSources, query, cancellationToken);
        var cheapReal = cheapResults.Where(r => !r.IsFallback).SelectMany(r => r.Offers).ToList();

        var expensiveResults = cheapReal.Count > 0
            ? []
            : await QueryAllAsync(expensiveSources, query, cancellationToken);

        var allResults = cheapResults.Concat(expensiveResults).ToList();
        var real = allResults.Where(r => !r.IsFallback).SelectMany(r => r.Offers).ToList();
        var fallback = allResults.Where(r => r.IsFallback).SelectMany(r => r.Offers).ToList();
        return (real, fallback);
    }

    private async Task<List<(bool IsFallback, IReadOnlyList<StoreOffer> Offers)>> QueryAllAsync(
        IReadOnlyList<IProductDataSource> sources, string query, CancellationToken cancellationToken)
    {
        var tasks = sources
            .Select(async source => (source.IsFallback, Offers: await QuerySourceAsync(source, query, cancellationToken)))
            .ToList();
        return (await Task.WhenAll(tasks)).ToList();
    }

    private async Task<IReadOnlyList<StoreOffer>> QuerySourceAsync(
        IProductDataSource source,
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            return await source.SearchAsync(query, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Data source {Source} failed for query {Query}; skipping it", source.SourceName, query);
            return Array.Empty<StoreOffer>();
        }
    }

    /// <summary>Sorts offers cheapest-first (spec FR4) and flags the lowest (spec FR5).</summary>
    private static List<AggregatedOffer> RankOffers(IEnumerable<StoreOffer> offers)
    {
        var sorted = offers
            .OrderBy(o => o.Price)
            .ThenBy(o => o.StoreName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ranked = new List<AggregatedOffer>(sorted.Count);
        for (var i = 0; i < sorted.Count; i++)
        {
            var offer = sorted[i];
            ranked.Add(new AggregatedOffer
            {
                StoreName = offer.StoreName,
                ProductTitle = offer.ProductTitle,
                Price = offer.Price,
                Currency = offer.Currency,
                ProductUrl = offer.ProductUrl,
                InStock = offer.InStock,
                ImageUrl = offer.ImageUrl,
                FreeShipping = offer.FreeShipping,
                DeliveryDays = offer.DeliveryDays,
                NormalizedKey = ProductNormalizer.Normalize(offer.ProductTitle),
                IsLowestPrice = i == 0,
            });
        }

        return ranked;
    }

    /// <summary>
    /// Builds the recommendation from ranked offers: the cheapest offer and how
    /// much it saves versus the most expensive one (spec FR6).
    /// </summary>
    private static Recommendation BuildRecommendation(IReadOnlyList<AggregatedOffer> ranked)
    {
        var best = ranked[0];
        var mostExpensive = ranked[^1];
        var savings = mostExpensive.Price - best.Price;

        var message = savings > 0
            ? $"Buy from {best.StoreName} at {best.Price:N2} {best.Currency} — save {savings:N2} {best.Currency} versus {mostExpensive.StoreName} at {mostExpensive.Price:N2} {mostExpensive.Currency}."
            : $"Best price is {best.Price:N2} {best.Currency} at {best.StoreName}.";

        return new Recommendation
        {
            BestStoreName = best.StoreName,
            BestPrice = best.Price,
            Currency = best.Currency,
            ComparedStoreName = mostExpensive.StoreName,
            ComparedPrice = mostExpensive.Price,
            Savings = savings,
            Message = message,
        };
    }
}
