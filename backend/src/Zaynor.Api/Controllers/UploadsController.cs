using Microsoft.AspNetCore.Mvc;
using Zaynor.Application.ImageSearch;

namespace Zaynor.Api.Controllers;

/// <summary>
/// Serves a photo uploaded to /api/search/by-image back out over HTTP —
/// deliberately anonymous, since Google's own crawler (not the browser) is
/// what fetches this URL for the reverse-image lookup. Safe to leave open:
/// entries are short-lived (a few minutes), capped at 8MB, image-only, and
/// keyed by an unguessable id — never a general-purpose file host.
/// </summary>
[ApiController]
[Route("api/uploads")]
public class UploadsController : ControllerBase
{
    private readonly ITempImageStore _store;

    public UploadsController(ITempImageStore store)
    {
        _store = store;
    }

    [HttpGet("{id}")]
    public ActionResult Get(string id)
    {
        var entry = _store.Get(id);
        if (entry is null)
        {
            return NotFound();
        }

        return File(entry.Value.Bytes, entry.Value.ContentType);
    }
}
