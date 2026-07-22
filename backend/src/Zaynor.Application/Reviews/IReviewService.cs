using Zaynor.Application.Reviews.Models;

namespace Zaynor.Application.Reviews;

/// <summary>
/// Store reviews — always public regardless of rating (spec: hiding
/// negative reviews would be deceptive). Unlimited reviews per user per
/// store (founder's call — no one-review-per-store restriction).
/// </summary>
public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetReviewsForStoreAsync(string storeName, CancellationToken cancellationToken = default);

    /// <summary>A small set of the highest-rated recent reviews across every store, for the homepage — a curated highlight, not a filter on what's visible elsewhere.</summary>
    Task<IReadOnlyList<ReviewDto>> GetFeaturedReviewsAsync(CancellationToken cancellationToken = default);

    /// <summary>Every review across every store, newest first — the admin's discovery list for replying.</summary>
    Task<IReadOnlyList<ReviewDto>> GetAllReviewsAsync(CancellationToken cancellationToken = default);

    Task<ReviewDto> SubmitReviewAsync(
        int userId, string storeName, int rating, string comment, string? displayName, CancellationToken cancellationToken = default);

    /// <summary>Sets/overwrites the admin's reply; null if the review doesn't exist.</summary>
    Task<ReviewDto?> ReplyAsync(int reviewId, string reply, CancellationToken cancellationToken = default);
}
