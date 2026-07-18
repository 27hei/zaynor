namespace Zaynor.Application.Auth.Models;

/// <summary>Payload to sign in to an existing account.</summary>
public sealed record LoginRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}
