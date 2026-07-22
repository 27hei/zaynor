namespace Zaynor.Application.Aggregation.Models;

/// <summary>
/// Rich detail fields already fetched during search via the Google Immersive
/// Product API (see GoogleShoppingDataSource) — never an extra paid API call,
/// just data we were already discarding. Null for every other source
/// (curated catalog, Rainforest Amazon, AliExpress) — never fabricated.
/// </summary>
public sealed record ProductDetails
{
    /// <summary>Product-level image gallery (shared across every store of this product).</summary>
    public IReadOnlyList<string>? Images { get; init; }

    public string? Brand { get; init; }

    /// <summary>Product-level description (Google's own "about this product" card) — general to the product, not this specific store.</summary>
    public string? Description { get; init; }

    /// <summary>Product-level spec bullets ("Title: Value"), capped to a reasonable count.</summary>
    public IReadOnlyList<string>? Specifications { get; init; }

    /// <summary>This store's own fulfillment bullets (e.g. "In stock online", "Delivery SAR 29", "3-day returns") — specific to this one offer, not the product in general.</summary>
    public IReadOnlyList<string>? StoreHighlights { get; init; }
}
