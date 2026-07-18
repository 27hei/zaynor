using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zaynor.Api.Extensions;
using Zaynor.Application.UserItems;
using Zaynor.Application.UserItems.Models;

namespace Zaynor.Api.Controllers;

/// <summary>A signed-in user's price-drop alert subscriptions (spec FR8).</summary>
[ApiController]
[Authorize]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    public sealed record CreateAlertRequest(string ProductName, decimal? PriceBaseline, string? Currency);

    private readonly IUserItemsService _userItems;

    public AlertsController(IUserItemsService userItems)
    {
        _userItems = userItems;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        return Ok(await _userItems.GetAlertsAsync(userId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAlertRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return BadRequest(new { error = "A product name is required." });
        }

        var alert = await _userItems.CreateAlertAsync(
            userId, request.ProductName, request.PriceBaseline, request.Currency, cancellationToken);

        return Ok(alert);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        var removed = await _userItems.RemoveAlertAsync(userId, id, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}
