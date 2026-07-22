using Microsoft.EntityFrameworkCore;
using Zaynor.Application.SiteReviews;
using Zaynor.Application.SiteReviews.Models;
using Zaynor.Domain.Entities;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.SiteReviews;

/// <summary>Database-backed reviews of Zaynor itself.</summary>
public sealed class SiteReviewService : ISiteReviewService
{
    private readonly ZaynorDbContext _db;

    public SiteReviewService(ZaynorDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SiteReviewDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Ordered by Id (monotonic) — SQLite cannot ORDER BY DateTimeOffset.
        return await _db.SiteReviews
            .OrderByDescending(r => r.Id)
            .Select(r => new SiteReviewDto
            {
                Id = r.Id,
                DisplayName = r.DisplayName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SiteReviewDto> SubmitAsync(
        int userId, int rating, string comment, string? displayName, CancellationToken cancellationToken = default)
    {
        var review = new SiteReview
        {
            UserId = userId,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            Rating = rating,
            Comment = comment.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.SiteReviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        return new SiteReviewDto
        {
            Id = review.Id,
            DisplayName = review.DisplayName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
        };
    }

    public async Task<bool> DeleteAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _db.SiteReviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return false;
        }

        _db.SiteReviews.Remove(review);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
