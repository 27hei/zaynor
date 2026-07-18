namespace Zaynor.Application.Auth.Models;

/// <summary>A successful auth result: the JWT and the authenticated user.</summary>
public sealed record AuthResponse
{
    public required string Token { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required UserDto User { get; init; }
}
