using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zaynor.Domain.Entities;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Api.Controllers;

/// <summary>
/// Outbound click tracking (spec Sections 10/20): logs every "go to store"
/// click — the metric affiliate networks value — then 302-redirects to the
/// store. Only known store domains are allowed, so this can never become an
/// open redirect. Affiliate tracking params ride through here once accounts
/// exist.
/// </summary>
[ApiController]
[Route("api/out")]
public class OutController : ControllerBase
{
    private static readonly string[] AllowedHosts =
    [
        "amazon.sa", "noon.com", "jarir.com", "extra.com", "aliexpress.com",
    ];

    private readonly ZaynorDbContext _db;
    private readonly ILogger<OutController> _logger;

    public OutController(ZaynorDbContext db, ILogger<OutController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Go(
        [FromQuery] string? u,
        [FromQuery] string? store,
        [FromQuery] string? product,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(u)
            || !Uri.TryCreate(u, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !AllowedHosts.Any(h => uri.Host == h || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { error = "Unknown store link." });
        }

        // Logging must never block the user's path to the store (NFR4).
        try
        {
            _db.ClickEvents.Add(new ClickEvent
            {
                StoreName = (store ?? uri.Host).Trim(),
                ProductName = (product ?? string.Empty).Trim(),
                Url = u,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Click logging failed; redirecting anyway");
        }

        return Redirect(u);
    }

    /// <summary>Total outbound clicks — evidence for affiliate applications.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken) =>
        Ok(new { totalClicks = await _db.ClickEvents.CountAsync(cancellationToken) });
}
