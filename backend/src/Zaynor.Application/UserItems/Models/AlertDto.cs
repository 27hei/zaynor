namespace Zaynor.Application.UserItems.Models;

/// <summary>A price-drop alert subscription (spec FR8).</summary>
public sealed record AlertDto
{
    public required int Id { get; init; }

    public required string ProductName { get; init; }

    /// <summary>The stored trigger condition, e.g. "price_drop_below:4237.52 SAR".</summary>
    public required string TargetCondition { get; init; }

    public required bool IsActive { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
