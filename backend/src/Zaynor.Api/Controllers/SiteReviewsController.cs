using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zaynor.Api.Extensions;
using Zaynor.Application.SiteReviews;
using Zaynor.Application.SiteReviews.Models;

namespace Zaynor.Api.Controllers;

/// <summary>
/// Reviews of Zaynor itself, shown publicly on the homepage. Deliberately
/// mixed public/authenticated (like ReviewsController) — reviews must
/// always be publicly readable. Unlike store reviews, an admin can delete
/// one outright (see SiteReview's remarks for why that's not the same as
/// hiding negative store feedback).
/// </summary>
[ApiController]
[Route("api/site-reviews")]
public class SiteReviewsController : ControllerBase
{
    private const int MaxCommentLength = 2000;

    public sealed record SubmitSiteReviewRequest(int Rating, string Comment, string? DisplayName);

    private readonly ISiteReviewService _siteReviews;

    public SiteReviewsController(ISiteReviewService siteReviews)
    {
        _siteReviews = siteReviews;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SiteReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _siteReviews.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("submission")]
    [ProducesResponseType(typeof(SiteReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] SubmitSiteReviewRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        if (request.Rating is < 1 or > 5)
        {
            return BadRequest(new { error = "rating must be between 1 and 5." });
        }

        if (string.IsNullOrWhiteSpace(request.Comment) || request.Comment.Length > MaxCommentLength)
        {
            return BadRequest(new { error = $"comment is required and must be under {MaxCommentLength} characters." });
        }

        return Ok(await _siteReviews.SubmitAsync(userId, request.Rating, request.Comment, request.DisplayName, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _siteReviews.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
