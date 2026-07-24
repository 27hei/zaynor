using Microsoft.Extensions.Logging;
using Zaynor.Application.Aggregation.Models;

namespace Zaynor.Application.Aggregation;

/// <summary>
/// Default aggregation engine. Fans a search out across every registered
/// <see cref="IProductDataSource"/> concurrently, merges the offers, ranks
/// them by a multi-factor score (see <see cref="OfferScorer"/>), flags the
/// cheapest, and builds a recommendation. Stateless per request (spec
/// Section 13).
///
/// Deliberately NOT <see cref="IAggregationService"/> itself (only its
/// decorator, <c>CachedAggregationService</c>, is bound to that interface in
/// DI) — this engine always computes the FULL result for a query and knows
/// nothing about pages; pagination is purely a slicing concern layered on
/// top of the one cached, fully-ranked result, so no page turn ever repeats
/// a live vendor call.
/// </summary>
public sealed class AggregationService
{
    private readonly IReadOnlyList<IProductDataSource> _sources;
    private readonly AffiliateSettings _affiliateSettings;
    private readonly RankingWeights _rankingWeights;
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(
        IEnumerable<IProductDataSource> sources,
        AffiliateSettings affiliateSettings,
        RankingWeights rankingWeights,
        ILogger<AggregationService> logger)
    {
        _sources = sources.ToList();
        _affiliateSettings = affiliateSettings;
        _rankingWeights = rankingWeights;
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

        var fetchedAt = DateTimeOffset.UtcNow;
        var merged = MergeDuplicateListings(offers);
        var ranked = RankOffers(merged, ProductNormalizer.Normalize(trimmed), _affiliateSettings, fetchedAt, _rankingWeights);
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
    /// expensive/live feeds alike — and merges their real offers together,
    /// pairing each with the confidence of the source that produced it
    /// (needed by OfferScorer, but not part of StoreOffer's own public
    /// shape — it's a property of the source, not the offer). Earlier this
    /// skipped live feeds entirely whenever the curated catalog had any
    /// match, to conserve their paid-API quota; that silently capped the
    /// handful of curated products (iPhone 15, Galaxy S24, PS5, ...) at
    /// just the 2-3 manually-entered stores instead of the dozens live
    /// search finds for everything else — the opposite of what's wanted
    /// (spec: founder's call — maximum store coverage matters more than
    /// quota conservation). A source that throws is logged and skipped so
    /// one failing source never breaks the whole search (spec NFR4).
    /// </summary>
    private async Task<(List<(StoreOffer Offer, double Confidence)> Real, List<(StoreOffer Offer, double Confidence)> Fallback)> GatherOffersAsync(
        string query, CancellationToken cancellationToken)
    {
        var cheapSources = _sources.Where(s => !s.IsExpensiveLive).ToList();
        var expensiveSources = _sources.Where(s => s.IsExpensiveLive).ToList();

        var cheapResultsTask = QueryAllAsync(cheapSources, query, cancellationToken);
        var expensiveResultsTask = QueryAllAsync(expensiveSources, query, cancellationToken);
        await Task.WhenAll(cheapResultsTask, expensiveResultsTask);

        var allResults = cheapResultsTask.Result.Concat(expensiveResultsTask.Result).ToList();
        var real = allResults
            .Where(r => !r.IsFallback)
            .SelectMany(r => r.Offers.Select(o => (Offer: o, r.Confidence)))
            .ToList();
        var fallback = allResults
            .Where(r => r.IsFallback)
            .SelectMany(r => r.Offers.Select(o => (Offer: o, r.Confidence)))
            .ToList();
        return (real, fallback);
    }

    private async Task<List<(bool IsFallback, IReadOnlyList<StoreOffer> Offers, double Confidence)>> QueryAllAsync(
        IReadOnlyList<IProductDataSource> sources, string query, CancellationToken cancellationToken)
    {
        var tasks = sources
            .Select(async source => (source.IsFallback, Offers: await QuerySourceAsync(source, query, cancellationToken), source.Confidence))
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
    /// Collapses the SAME physical listing when two or more independent
    /// sources return it (e.g. DataForSeoAmazonDataSource and
    /// BrightDataAmazonDataSource both returning the identical Amazon.sa
    /// ASIN) into one merged record — but, unlike the old store-wide
    /// collapse this replaces, genuinely different listings from the same
    /// store now survive as separate entries (a store can show more than
    /// one listing per search). Grouped by (store, vendor id if known, else
    /// normalized title) — four sources deliberately all hardcode
    /// StoreName = "Amazon.sa" (real production redundancy so one vendor's
    /// outage doesn't remove Amazon from results entirely, see
    /// OxylabsAmazonDataSource's remarks), so this is where their overlap
    /// actually gets reconciled. Within a duplicate group, the cheapest
    /// price wins as canonical, but fields it's missing (image/rating/
    /// review count/rich details) are backfilled from siblings that have
    /// them — a vendor lacking an image shouldn't cost the merged listing
    /// its image just because it happened to be cheapest.
    /// </summary>
    private static List<(StoreOffer Offer, double Confidence)> MergeDuplicateListings(
        IEnumerable<(StoreOffer Offer, double Confidence)> offers)
    {
        var groups = new Dictionary<string, List<(StoreOffer Offer, double Confidence)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in offers)
        {
            var dedupKey = entry.Offer.ExternalId ?? ProductNormalizer.Normalize(entry.Offer.ProductTitle);
            var groupKey = $"{entry.Offer.StoreName}::{dedupKey}";
            if (!groups.TryGetValue(groupKey, out var list))
            {
                list = [];
                groups[groupKey] = list;
            }

            list.Add(entry);
        }

        var merged = new List<(StoreOffer Offer, double Confidence)>(groups.Count);
        foreach (var group in groups.Values)
        {
            if (group.Count == 1)
            {
                merged.Add(group[0]);
                continue;
            }

            var canonical = group.OrderBy(g => g.Offer.Price).First();
            var mergedOffer = canonical.Offer with
            {
                ImageUrl = canonical.Offer.ImageUrl ?? group.Select(g => g.Offer.ImageUrl).FirstOrDefault(v => v is not null),
                Rating = canonical.Offer.Rating ?? group.Select(g => g.Offer.Rating).FirstOrDefault(v => v is not null),
                RatingCount = canonical.Offer.RatingCount ?? group.Select(g => g.Offer.RatingCount).FirstOrDefault(v => v is not null),
                ProductDetails = canonical.Offer.ProductDetails ?? group.Select(g => g.Offer.ProductDetails).FirstOrDefault(v => v is not null),
            };
            merged.Add((mergedOffer, canonical.Confidence));
        }

        return merged;
    }

    /// <summary>
    /// Scores every offer via <see cref="OfferScorer"/> and sorts
    /// highest-score-first (replacing plain cheapest-first). Flags the
    /// single true cheapest offer (spec FR5) independently of rank order —
    /// with score-based ranking, the top-ranked offer is not necessarily the
    /// cheapest one.
    /// </summary>
    private static List<AggregatedOffer> RankOffers(
        IReadOnlyList<(StoreOffer Offer, double Confidence)> merged,
        string normalizedQuery,
        AffiliateSettings affiliateSettings,
        DateTimeOffset fetchedAt,
        RankingWeights weights)
    {
        var offers = merged.Select(m => m.Offer).ToList();
        var confidences = merged.Select(m => m.Confidence).ToList();
        var scores = OfferScorer.ScoreAll(offers, confidences, normalizedQuery, weights);

        var order = Enumerable.Range(0, merged.Count)
            .OrderByDescending(i => scores[i])
            .ThenBy(i => offers[i].StoreName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ranked = new List<AggregatedOffer>(order.Count);
        foreach (var i in order)
        {
            var offer = offers[i];
            var normalizedKey = ProductNormalizer.Normalize(offer.ProductTitle);
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
                NormalizedKey = normalizedKey,
                IsLowestPrice = false, // resolved below, once every offer exists to compare
                HasAffiliateLink = AffiliateEligibility.IsMonetized(offer.ProductUrl, affiliateSettings),
                Signature = offer.Signature,
                ProductDetails = offer.ProductDetails,
                ExternalId = offer.ExternalId,
                ListingId = $"{offer.StoreName}:{offer.ExternalId ?? normalizedKey}",
                FetchedAt = fetchedAt,
                Score = scores[i],
            });
        }

        if (ranked.Count > 0)
        {
            var lowestIndex = 0;
            for (var i = 1; i < ranked.Count; i++)
            {
                if (ranked[i].Price < ranked[lowestIndex].Price
                    || (ranked[i].Price == ranked[lowestIndex].Price
                        && string.Compare(ranked[i].StoreName, ranked[lowestIndex].StoreName, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    lowestIndex = i;
                }
            }

            ranked[lowestIndex] = ranked[lowestIndex] with { IsLowestPrice = true };
        }

        return ranked;
    }

    /// <summary>
    /// Builds the recommendation from ranked offers: the cheapest offer and how
    /// much it saves versus the most expensive one (spec FR6). Finds both
    /// explicitly by price — ranking is score-based now, so the first/last
    /// entries in rank order are not guaranteed to be the price extremes.
    /// </summary>
    private static Recommendation BuildRecommendation(IReadOnlyList<AggregatedOffer> ranked)
    {
        var best = ranked.MinBy(o => o.Price)!;
        var mostExpensive = ranked.MaxBy(o => o.Price)!;
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
