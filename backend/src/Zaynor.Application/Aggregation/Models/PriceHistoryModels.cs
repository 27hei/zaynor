namespace Zaynor.Application.Aggregation.Models;

/// <summary>One observed price point for a product at a store.</summary>
public sealed record PriceHistoryPoint
{
    public required string StoreName { get; init; }

    public required decimal Price { get; init; }

    public required DateTimeOffset RecordedAt { get; init; }
}

/// <summary>
/// The accumulated price history for a searched product (spec Section 15;
/// competitive analysis table stakes #5). Empty until searches have recorded
/// observations — the UI states that honestly.
/// </summary>
public sealed record PriceHistoryResponse
{
    /// <summary>The matched product's canonical name, or null when unknown.</summary>
    public string? ProductName { get; init; }

    public required IReadOnlyList<PriceHistoryPoint> Points { get; init; }
}
