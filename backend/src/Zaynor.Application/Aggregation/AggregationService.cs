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
    private readonly AffiliateSettings _affiliateSettings;
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(
        IEnumerable<IProductDataSource> sources,
        AffiliateSettings affiliateSettings,
        ILogger<AggregationService> logger)
    {
        _sources = sources.ToList();
        _affiliateSettings = affiliateSettings;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return new SearchResult { Query = string.Empty, Offers = Array.Empty<AggregatedOffer>() };
        }

        // Surfaced to the UI (a "Showing results for X" style note) whenever
        // a colloquial Arabic brand spelling ("سامسنج" → "Samsung") was
        // corrected before searching, so the correction stays visible
        // instead of only happening silently inside the live data source.
        var normalized = ArabicBrandNormalizer.Normalize(trimmed);
        var correctedQuery = normalized != trimmed ? normalized : null;

        var (realOffers, fallbackOffers) = await GatherOffersAsync(trimmed, cancellationToken);

        // Real sources win outright; fallback (demo) offers appear only when
        // nothing real covered the query, and are flagged as demo data.
        var isDemo = realOffers.Count == 0 && fallbackOffers.Count > 0;
        var offers = realOffers.Count > 0 ? realOffers : fallbackOffers;

        if (offers.Count == 0)
        {
            _logger.LogInformation("No offers found for query {Query}", trimmed);
            return new SearchResult { Query = trimmed, Offers = Array.Empty<AggregatedOffer>(), CorrectedQuery = correctedQuery };
        }

        var ranked = RankOffers(DeduplicateByStore(offers), _affiliateSettings);
        var recommendation = BuildRecommendation(ranked);

        return new SearchResult
        {
            Query = trimmed,
            Offers = ranked,
            Recommendation = recommendation,
            IsDemoData = isDemo,
            CorrectedQuery = correctedQuery,
        };
    }

    /// <summary>
    /// Queries every source concurrently — cheap (curated catalog) and
    /// expensive/live feeds alike — and merges their real offers together.
    /// Earlier this skipped live feeds entirely whenever the curated catalog
    /// had any match, to conserve their paid-API quota; that silently capped
    /// the handful of curated products (iPhone 15, Galaxy S24, PS5, ...) at
    /// just the 2-3 manually-entered stores instead of the dozens live
    /// search finds for everything else — the opposite of what's wanted
    /// (spec: founder's call — maximum store coverage matters more than
    /// quota conservation). A source that throws is logged and skipped so
    /// one failing source never breaks the whole search (spec NFR4).
    /// </summary>
    private async Task<(List<StoreOffer> Real, List<StoreOffer> Fallback)> GatherOffersAsync(
        string query, CancellationToken cancellationToken)
    {
        var cheapSources = _sources.Where(s => !s.IsExpensiveLive).ToList();
        var expensiveSources = _sources.Where(s => s.IsExpensiveLive).ToList();

        var cheapResultsTask = QueryAllAsync(cheapSources, query, cancellationToken);
        var expensiveResultsTask = QueryAllAsync(expensiveSources, query, cancellationToken);
        await Task.WhenAll(cheapResultsTask, expensiveResultsTask);

        var allResults = cheapResultsTask.Result.Concat(expensiveResultsTask.Result).ToList();
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
        // A source's own HttpClient.Timeout firing also throws an
        // OperationCanceledException (TaskCanceledException) — distinguish
        // that from the caller's own cancellationToken being cancelled so a
        // single slow upstream API degrades gracefully instead of aborting
        // the whole search.
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Data source {Source} timed out for query {Query}; skipping it", source.SourceName, query);
            return Array.Empty<StoreOffer>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Data source {Source} failed for query {Query}; skipping it", source.SourceName, query);
            return Array.Empty<StoreOffer>();
        }
    }

    /// <summary>
    /// Keeps the cheapest offer per store name across ALL sources combined.
    /// Individual sources already dedupe within themselves (e.g.
    /// GoogleShoppingDataSource's own bestPerMerchant), but with more than one
    /// independent source now able to return the same store (RainforestAmazonDataSource,
    /// DataForSeoAmazonDataSource, and OxylabsAmazonDataSource all hardcode
    /// StoreName = "Amazon.sa" — a deliberate design: real production
    /// redundancy so one vendor's outage doesn't remove Amazon from results
    /// entirely, see OxylabsAmazonDataSource's remarks), nothing previously
    /// stopped the same store from appearing twice with two different prices
    /// once a second source went live. A user comparing prices "across
    /// stores" should never see one store listed twice.
    /// </summary>
    private static List<StoreOffer> DeduplicateByStore(IEnumerable<StoreOffer> offers)
    {
        var bestPerStore = new Dictionary<string, StoreOffer>(StringComparer.OrdinalIgnoreCase);
        foreach (var offer in offers)
        {
            if (!bestPerStore.TryGetValue(offer.StoreName, out var existing) || offer.Price < existing.Price)
            {
                bestPerStore[offer.StoreName] = offer;
            }
        }

        return bestPerStore.Values.ToList();
    }

    /// <summary>Sorts offers cheapest-first (spec FR4) and flags the lowest (spec FR5).</summary>
    private static List<AggregatedOffer> RankOffers(IEnumerable<StoreOffer> offers, AffiliateSettings affiliateSettings)
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
                Rating = offer.Rating,
                RatingCount = offer.RatingCount,
                NormalizedKey = ProductNormalizer.Normalize(offer.ProductTitle),
                IsLowestPrice = i == 0,
                HasAffiliateLink = AffiliateEligibility.IsMonetized(offer.ProductUrl, affiliateSettings),
                Signature = offer.Signature,
                ProductDetails = offer.ProductDetails,
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
