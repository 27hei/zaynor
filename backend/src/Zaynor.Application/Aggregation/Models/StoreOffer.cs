namespace Zaynor.Application.Aggregation.Models;

/// <summary>
/// A normalized offer returned by an <see cref="IProductDataSource"/> — the
/// raw input the aggregation engine ranks and merges. Each data source is
/// responsible for mapping its own feed/API shape into this common form.
/// </summary>
public sealed record StoreOffer
{
    public required string StoreName { get; init; }

    public required string ProductTitle { get; init; }

    public required decimal Price { get; init; }

    public required string Currency { get; init; }

    /// <summary>Outbound (affiliate tracking) URL to the store's product page.</summary>
    public required string ProductUrl { get; init; }

    public bool InStock { get; init; } = true;

    public string? ImageUrl { get; init; }

    /// <summary>Whether the store ships this offer for free (shown in result rows).</summary>
    public bool FreeShipping { get; init; }

    /// <summary>Estimated delivery time in days, or null when unknown.</summary>
    public int? DeliveryDays { get; init; }

    /// <summary>Star rating (0-5) when the source provides one; null otherwise (never fabricated).</summary>
    public decimal? Rating { get; init; }

    /// <summary>Number of ratings behind <see cref="Rating"/>, or null when unknown.</summary>
    public int? RatingCount { get; init; }

    /// <summary>
    /// Set only by sources whose merchant domains aren't on <c>/api/out</c>'s
    /// static known-domain list (currently: <c>GoogleShoppingDataSource</c>,
    /// which resolves real links to an open-ended set of stores via the
    /// Immersive Product API) — proves the URL came from a real search
    /// result rather than an attacker-supplied redirect target. See
    /// <see cref="OutboundLinkSigner"/>.
    /// </summary>
    public string? Signature { get; init; }

    /// <summary>Rich detail fields already fetched during search (GoogleShoppingDataSource only) — null for every other source, never fabricated.</summary>
    public ProductDetails? ProductDetails { get; init; }
}
