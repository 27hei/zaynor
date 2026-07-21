using Microsoft.AspNetCore.Mvc;
using Zaynor.Application.ImageSearch;

namespace Zaynor.Api.Controllers;

/// <summary>
/// "Search by photo": takes an uploaded image, resolves it to a product name
/// via reverse image search, and hands that back so the frontend runs it
/// through the normal /api/search — image search shares the exact same
/// aggregation pipeline as a typed query, never a separate results path.
/// </summary>
[ApiController]
[Route("api/search")]
public class ImageSearchController : ControllerBase
{
    private const long MaxImageBytes = 8 * 1024 * 1024; // 8MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };

    private readonly ITempImageStore _store;
    private readonly IImageQueryResolver _resolver;

    public ImageSearchController(ITempImageStore store, IImageQueryResolver resolver)
    {
        _store = store;
        _resolver = resolver;
    }

    [HttpPost("by-image")]
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<ActionResult> SearchByImage(IFormFile? image, CancellationToken cancellationToken)
    {
        if (!_resolver.IsEnabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Image search isn't available right now." });
        }

        if (image is null || image.Length == 0)
        {
            return BadRequest(new { error = "An image file is required." });
        }

        if (image.Length > MaxImageBytes)
        {
            return BadRequest(new { error = "That image is too large (max 8MB)." });
        }

        if (!AllowedContentTypes.Contains(image.ContentType))
        {
            return BadRequest(new { error = "Unsupported image type — use JPEG, PNG, or WEBP." });
        }

        using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer, cancellationToken);

        var id = _store.Save(buffer.ToArray(), image.ContentType);
        var publicUrl = $"{Request.Scheme}://{Request.Host}/api/uploads/{id}";

        var query = await _resolver.ResolveQueryAsync(publicUrl, cancellationToken);
        if (string.IsNullOrWhiteSpace(query))
        {
            return NotFound(new { error = "Couldn't recognize a product in that photo." });
        }

        return Ok(new { query });
    }
}
