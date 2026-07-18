namespace Zaynor.Application.Aggregation.Models;

/// <summary>
/// The recommendation shown to the user: the best offer, how much it saves
/// versus the most expensive offer, and a human-readable message (spec FR6).
/// </summary>
public sealed record Recommendation
{
    public required string BestStoreName { get; init; }

    public required decimal BestPrice { get; init; }

    public required string Currency { get; init; }

    /// <summary>The store the saving is measured against (the most expensive offer).</summary>
    public required string ComparedStoreName { get; init; }

    public required decimal ComparedPrice { get; init; }

    /// <summary>Maximum saving versus the most expensive offer (ComparedPrice - BestPrice).</summary>
    public required decimal Savings { get; init; }

    public required string Message { get; init; }
}
