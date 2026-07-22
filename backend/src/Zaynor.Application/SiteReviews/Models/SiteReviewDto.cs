namespace Zaynor.Application.SiteReviews.Models;

/// <summary>A customer review of Zaynor itself — shown publicly on the homepage.</summary>
public sealed record SiteReviewDto
{
    public required int Id { get; init; }

    public string? DisplayName { get; init; }

    public required int Rating { get; init; }

    public required string Comment { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
