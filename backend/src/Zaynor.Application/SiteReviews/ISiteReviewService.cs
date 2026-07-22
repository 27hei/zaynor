using Zaynor.Application.SiteReviews.Models;

namespace Zaynor.Application.SiteReviews;

/// <summary>
/// Reviews of Zaynor itself (not a specific store) — always public. Unlike
/// store reviews, the admin may delete one outright (moderating content
/// about the founder's own platform, not suppressing feedback about a
/// third-party store a shopper is deciding whether to trust).
/// </summary>
public interface ISiteReviewService
{
    Task<IReadOnlyList<SiteReviewDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SiteReviewDto> SubmitAsync(int userId, int rating, string comment, string? displayName, CancellationToken cancellationToken = default);

    /// <summary>False when the review doesn't exist.</summary>
    Task<bool> DeleteAsync(int reviewId, CancellationToken cancellationToken = default);
}
