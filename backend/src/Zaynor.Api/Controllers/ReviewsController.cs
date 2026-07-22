using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zaynor.Api.Extensions;
using Zaynor.Application.Reviews;
using Zaynor.Application.Reviews.Models;

namespace Zaynor.Api.Controllers;

/// <summary>
/// Store reviews. Deliberately mixed public/authenticated (unlike the
/// uniform class-level [Authorize] on SavedProducts/Alerts) — reviews must
/// always be publicly readable, even for a logged-out visitor.
/// </summary>
[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private const int MaxCommentLength = 2000;

    public sealed record SubmitReviewRequest(string StoreName, int Rating, string Comment, string? DisplayName);

    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] string? storeName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storeName))
        {
            return BadRequest(new { error = "storeName is required." });
        }

        return Ok(await _reviews.GetReviewsForStoreAsync(storeName, cancellationToken));
    }

    /// <summary>A small curated highlight for the homepage — see IReviewService.GetFeaturedReviewsAsync remarks.</summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Featured(CancellationToken cancellationToken) =>
        Ok(await _reviews.GetFeaturedReviewsAsync(cancellationToken));

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("submission")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.StoreName))
        {
            return BadRequest(new { error = "storeName is required." });
        }

        if (request.Rating is < 1 or > 5)
        {
            return BadRequest(new { error = "rating must be between 1 and 5." });
        }

        if (string.IsNullOrWhiteSpace(request.Comment) || request.Comment.Length > MaxCommentLength)
        {
            return BadRequest(new { error = $"comment is required and must be under {MaxCommentLength} characters." });
        }

        return Ok(await _reviews.SubmitReviewAsync(
            userId, request.StoreName, request.Rating, request.Comment, request.DisplayName, cancellationToken));
    }
}
