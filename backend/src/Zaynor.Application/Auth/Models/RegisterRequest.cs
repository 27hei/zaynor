namespace Zaynor.Application.Auth.Models;

/// <summary>Payload to create a new account.</summary>
public sealed record RegisterRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    /// <summary>Preferred UI locale, "ar" or "en" (spec NFR5).</summary>
    public string Locale { get; init; } = "ar";
}
