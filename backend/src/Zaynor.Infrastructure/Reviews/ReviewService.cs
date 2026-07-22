using Microsoft.EntityFrameworkCore;
using Zaynor.Application.Reviews;
using Zaynor.Application.Reviews.Models;
using Zaynor.Domain.Entities;
using Zaynor.Infrastructure.Aggregation;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.Reviews;

/// <summary>Database-backed store reviews. Every review is public regardless of rating.</summary>
public sealed class ReviewService : IReviewService
{
    private const int FeaturedCount = 8;
    private const int MinFeaturedRating = 4;

    private readonly ZaynorDbContext _db;

    public ReviewService(ZaynorDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReviewDto>> GetReviewsForStoreAsync(string storeName, CancellationToken cancellationToken = default)
    {
        var store = await StoreLookup.FindAsync(_db, storeName, cancellationToken);
        if (store is null)
        {
            return [];
        }

        // Ordered by Id (monotonic) — SQLite cannot ORDER BY DateTimeOffset.
        return await _db.Reviews
            .Where(r => r.StoreId == store.Id)
            .OrderByDescending(r => r.Id)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                StoreName = store.Name,
                DisplayName = r.DisplayName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                AdminReply = r.AdminReply,
                AdminReplyAt = r.AdminReplyAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewDto>> GetFeaturedReviewsAsync(CancellationToken cancellationToken = default)
    {
        // A curated highlight for the homepage, not a filter on what's
        // visible elsewhere — the full unfiltered list per store still
        // stays reachable via GetReviewsForStoreAsync, so nothing is hidden.
        return await _db.Reviews
            .Where(r => r.Rating >= MinFeaturedRating)
            .Join(_db.Stores, r => r.StoreId, s => s.Id, (r, s) => new ReviewDto
            {
                Id = r.Id,
                StoreName = s.Name,
                DisplayName = r.DisplayName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                AdminReply = r.AdminReply,
                AdminReplyAt = r.AdminReplyAt,
            })
            .OrderByDescending(dto => dto.Rating)
            .ThenByDescending(dto => dto.Id)
            .Take(FeaturedCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewDto>> GetAllReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Reviews
            .Join(_db.Stores, r => r.StoreId, s => s.Id, (r, s) => new ReviewDto
            {
                Id = r.Id,
                StoreName = s.Name,
                DisplayName = r.DisplayName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                AdminReply = r.AdminReply,
                AdminReplyAt = r.AdminReplyAt,
            })
            .OrderByDescending(dto => dto.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReviewDto> SubmitReviewAsync(
        int userId, string storeName, int rating, string comment, string? displayName, CancellationToken cancellationToken = default)
    {
        var store = await StoreLookup.FindOrCreateAsync(_db, storeName, baseUrl: null, cancellationToken);

        var review = new Review
        {
            StoreId = store.Id,
            UserId = userId,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            Rating = rating,
            Comment = comment.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(review, store.Name);
    }

    public async Task<ReviewDto?> ReplyAsync(int reviewId, string reply, CancellationToken cancellationToken = default)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return null;
        }

        review.AdminReply = reply.Trim();
        review.AdminReplyAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var store = await _db.Stores.FirstAsync(s => s.Id == review.StoreId, cancellationToken);
        return ToDto(review, store.Name);
    }

    private static ReviewDto ToDto(Review review, string storeName) => new()
    {
        Id = review.Id,
        StoreName = storeName,
        DisplayName = review.DisplayName,
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAt = review.CreatedAt,
        AdminReply = review.AdminReply,
        AdminReplyAt = review.AdminReplyAt,
    };
}
