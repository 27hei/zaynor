using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zaynor.Application.Reviews;
using Zaynor.Application.Reviews.Models;

namespace Zaynor.Api.Controllers;

/// <summary>Admin-only review management: discover every review to reply to, and post a public reply.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/reviews")]
public class AdminReviewsController : ControllerBase
{
    private const int MaxReplyLength = 2000;

    public sealed record ReplyRequest(string Reply);

    private readonly IReviewService _reviews;

    public AdminReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _reviews.GetAllReviewsAsync(cancellationToken));

    [HttpPost("{id:int}/reply")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reply) || request.Reply.Length > MaxReplyLength)
        {
            return BadRequest(new { error = $"reply is required and must be under {MaxReplyLength} characters." });
        }

        var result = await _reviews.ReplyAsync(id, request.Reply, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
