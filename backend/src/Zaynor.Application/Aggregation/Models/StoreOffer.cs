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
}
