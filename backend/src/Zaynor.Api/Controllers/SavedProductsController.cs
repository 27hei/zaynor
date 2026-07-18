using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zaynor.Api.Extensions;
using Zaynor.Application.UserItems;
using Zaynor.Application.UserItems.Models;

namespace Zaynor.Api.Controllers;

/// <summary>A signed-in user's saved products (spec FR9).</summary>
[ApiController]
[Authorize]
[Route("api/saved")]
public class SavedProductsController : ControllerBase
{
    public sealed record SaveProductRequest(string ProductName);

    private readonly IUserItemsService _userItems;

    public SavedProductsController(IUserItemsService userItems)
    {
        _userItems = userItems;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SavedProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        return Ok(await _userItems.GetSavedProductsAsync(userId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SavedProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Save([FromBody] SaveProductRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return BadRequest(new { error = "A product name is required." });
        }

        return Ok(await _userItems.SaveProductAsync(userId, request.ProductName, cancellationToken));
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

        var removed = await _userItems.RemoveSavedProductAsync(userId, id, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}
