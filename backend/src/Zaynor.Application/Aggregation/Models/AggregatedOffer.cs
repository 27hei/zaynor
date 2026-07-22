namespace Zaynor.Application.Aggregation.Models;

/// <summary>
/// An offer as presented in search results: a <see cref="StoreOffer"/> enriched
/// with the product-matching key and a flag marking the cheapest offer (spec FR5).
/// </summary>
public sealed record AggregatedOffer
{
    public required string StoreName { get; init; }

    public required string ProductTitle { get; init; }

    public required decimal Price { get; init; }

    public required string Currency { get; init; }

    public required string ProductUrl { get; init; }

    public bool InStock { get; init; } = true;

    public string? ImageUrl { get; init; }

    /// <summary>Whether the store ships this offer for free (spec Section 16: per-store row shows shipping).</summary>
    public bool FreeShipping { get; init; }

    /// <summary>Estimated delivery time in days, or null when unknown.</summary>
    public int? DeliveryDays { get; init; }

    /// <summary>Star rating (0-5) when the source provides one; null otherwise (never fabricated).</summary>
    public decimal? Rating { get; init; }

    /// <summary>Number of ratings behind <see cref="Rating"/>, or null when unknown.</summary>
    public int? RatingCount { get; init; }

    /// <summary>Normalized matching key for this offer's product (spec FR3).</summary>
    public required string NormalizedKey { get; init; }

    /// <summary>True for the single cheapest offer in the result set.</summary>
    public bool IsLowestPrice { get; init; }

    /// <summary>
    /// True when clicking through to this specific store right now actually
    /// carries an affiliate tag (computed the same way <c>OutController</c>
    /// decides whether to tag the outbound link) — never a hint about which
    /// offer is the best deal, only which one currently supports Zaynor.
    /// </summary>
    public bool HasAffiliateLink { get; init; }

    /// <summary>
    /// Proves to <c>/api/out</c> that this exact URL came from our own search
    /// results, not an attacker-supplied redirect target — set for stores
    /// outside the static known-domain list (spec: real stores like Mazeed,
    /// LetsTango, desertcart, ... discovered live via the Immersive Product
    /// API, an open-ended set impossible to allowlist by domain ahead of
    /// time). Null for sources whose domains are already on that static
    /// list (Amazon, Noon, curated catalog, ...) — no signature needed
    /// there. See <see cref="OutboundLinkSigner"/>.
    /// </summary>
    public string? Signature { get; init; }

    /// <summary>Rich detail fields already fetched during search (GoogleShoppingDataSource only) — null for every other source, never fabricated.</summary>
    public ProductDetails? ProductDetails { get; init; }
}
