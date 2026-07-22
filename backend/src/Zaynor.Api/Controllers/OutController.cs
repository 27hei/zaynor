using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zaynor.Application.Aggregation;
using Zaynor.Domain.Entities;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Api.Controllers;

/// <summary>
/// Outbound click tracking (spec Sections 10/20): logs every "go to store"
/// click — the metric affiliate networks value — then 302-redirects to the
/// store. A destination is trusted either because its domain is on the
/// static known-store list below, or because it carries a valid signature
/// (see <see cref="OutboundLinkSigner"/>) proving it came from our own
/// search results — real merchants the Immersive Product API resolves live
/// (Mazeed, LetsTango, desertcart, ...) are an open-ended set impossible to
/// allowlist by domain ahead of time, so the signature is what keeps this
/// from becoming an open redirect for those, the same way the static list
/// does for everyone else.
/// </summary>
[ApiController]
[Route("api/out")]
public class OutController : ControllerBase
{
    private static readonly string[] AllowedHosts =
    [
        "amazon.sa", "noon.com", "jarir.com", "extra.com", "aliexpress.com", "ebay.com",
    ];

    private readonly ZaynorDbContext _db;
    private readonly ILogger<OutController> _logger;
    private readonly string? _amazonTag;
    private readonly string? _noonUtmSuffix;
    private readonly string? _deeplinkTemplate;
    private readonly string[] _deeplinkHosts;
    private readonly string _linkSigningKey;

    public OutController(ZaynorDbContext db, ILogger<OutController> logger, IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _amazonTag = configuration["Affiliate:AmazonTag"];
        // Same key GoogleShoppingDataSource signs with (see OutboundLinkSigner remarks).
        _linkSigningKey = configuration["Jwt:Key"] ?? string.Empty;

        // Noon tags per-URL via query params appended directly to the same
        // noon.com link (utm_campaign/utm_medium/utm_source from the noon
        // partners dashboard) — not a redirector-wrapped deeplink, so it's a
        // simple suffix like the Amazon tag rather than a {url} template.
        _noonUtmSuffix = configuration["Affiliate:NoonUtmSuffix"];

        // Network deeplink wrapping (Admitad/ArabClicks style): a template with
        // {url} placeholder, applied to the configured store hosts. Left empty
        // until the network approves — activation is then a config change only.
        _deeplinkTemplate = configuration["Affiliate:DeeplinkTemplate"];
        _deeplinkHosts = (configuration["Affiliate:DeeplinkHosts"] ?? "jarir.com,extra.com,aliexpress.com")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    [HttpGet]
    public async Task<IActionResult> Go(
        [FromQuery] string? u,
        [FromQuery] string? store,
        [FromQuery] string? product,
        [FromQuery] string? sig,
        CancellationToken cancellationToken)
    {
        var isKnownHost = Uri.TryCreate(u, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && AllowedHosts.Any(h => uri.Host == h || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));
        var isSignedByUs = uri is not null && OutboundLinkSigner.Verify(u!, sig, _linkSigningKey);

        if (string.IsNullOrWhiteSpace(u) || uri is null || uri.Scheme != Uri.UriSchemeHttps || !(isKnownHost || isSignedByUs))
        {
            return BadRequest(new { error = "Unknown store link." });
        }

        // Affiliate monetization (spec Section 10): Amazon links carry the
        // Associates tag so qualifying purchases earn commission.
        var target = u;
        if (!string.IsNullOrWhiteSpace(_amazonTag)
            && (uri.Host == "amazon.sa" || uri.Host.EndsWith(".amazon.sa", StringComparison.OrdinalIgnoreCase))
            && !HasTagParameter(uri))
        {
            target = u + (u.Contains('?') ? "&" : "?") + "tag=" + Uri.EscapeDataString(_amazonTag);
        }
        else if (!string.IsNullOrWhiteSpace(_noonUtmSuffix)
            && (uri.Host == "noon.com" || uri.Host == "www.noon.com")
            && !uri.Query.Contains("utm_campaign", StringComparison.OrdinalIgnoreCase))
        {
            target = u + (u.Contains('?') ? "&" : "?") + _noonUtmSuffix;
        }
        else if (!string.IsNullOrWhiteSpace(_deeplinkTemplate)
            && _deeplinkTemplate.Contains("{url}")
            && _deeplinkHosts.Any(h => uri.Host == h || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase)))
        {
            // Network deeplink: the store URL rides inside the tracking link.
            target = _deeplinkTemplate.Replace("{url}", Uri.EscapeDataString(u));
        }

        // Logging must never block the user's path to the store (NFR4).
        try
        {
            _db.ClickEvents.Add(new ClickEvent
            {
                StoreName = (store ?? uri.Host).Trim(),
                ProductName = (product ?? string.Empty).Trim(),
                Url = target,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Click logging failed; redirecting anyway");
        }

        return Redirect(target);
    }

    /// <summary>
    /// True only when the URL already carries a real <c>tag</c> query parameter.
    /// Checked precisely (not a substring scan) because live Amazon search URLs
    /// contain <c>dib_tag=se</c>, which contains "tag=" but is NOT the Associates
    /// tag — a substring check there would wrongly skip adding our tag and lose
    /// the commission.
    /// </summary>
    private static bool HasTagParameter(Uri uri)
    {
        // Normalize so every parameter is preceded by '&', then "&tag=" matches
        // only a real tag param (never "&dib_tag=").
        var normalized = "&" + uri.Query.TrimStart('?');
        return normalized.Contains("&tag=", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Total outbound clicks — evidence for affiliate applications.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken) =>
        Ok(new { totalClicks = await _db.ClickEvents.CountAsync(cancellationToken) });
}
