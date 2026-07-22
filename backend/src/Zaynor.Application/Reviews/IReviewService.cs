using Zaynor.Application.Reviews.Models;

namespace Zaynor.Application.Reviews;

/// <summary>
/// Store reviews — public regardless of rating by default (spec: hiding
/// negative reviews would be deceptive; the admin's primary tool is a public
/// reply, not silent removal). Unlimited reviews per user per store
/// (founder's call — no one-review-per-store restriction). The admin can
/// still delete a review outright (e.g. test data, spam, abuse) — a
/// deliberate, explicit override of the "never hide" default, granted
/// because the founder is the sole moderator and accepted the trade-off
/// that this also permits deleting genuine negative feedback.
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

    /// <summary>Admin-only removal. Returns false if the review doesn't exist.</summary>
    Task<bool> DeleteAsync(int reviewId, CancellationToken cancellationToken = default);
}
