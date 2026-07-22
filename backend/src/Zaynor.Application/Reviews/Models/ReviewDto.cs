namespace Zaynor.Application.Reviews.Models;

/// <summary>A customer review of a store (spec: reviews are always public, admin may reply).</summary>
public sealed record ReviewDto
{
    public required int Id { get; init; }

    public required string StoreName { get; init; }

    public string? DisplayName { get; init; }

    public required int Rating { get; init; }

    public required string Comment { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public string? AdminReply { get; init; }

    public DateTimeOffset? AdminReplyAt { get; init; }
}
