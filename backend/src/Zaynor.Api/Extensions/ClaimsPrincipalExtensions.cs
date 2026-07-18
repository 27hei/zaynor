using System.Security.Claims;

namespace Zaynor.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the authenticated user's id from the JWT subject claim, or null.</summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return int.TryParse(subject, out var userId) ? userId : null;
    }
}
