namespace Zaynor.Infrastructure.Auth;

/// <summary>Strongly-typed JWT configuration, bound from the "Jwt" config section.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Zaynor";

    public string Audience { get; set; } = "ZaynorClient";

    /// <summary>Signing key. In production this must come from a secret store, not appsettings.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60 * 24 * 7; // 7 days
}
