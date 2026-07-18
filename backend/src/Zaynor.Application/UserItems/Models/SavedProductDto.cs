namespace Zaynor.Application.UserItems.Models;

/// <summary>A product a user has saved (spec FR9).</summary>
public sealed record SavedProductDto
{
    public required int Id { get; init; }

    public required string ProductName { get; init; }

    public required DateTimeOffset SavedAt { get; init; }
}
