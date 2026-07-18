namespace Zaynor.Application.Auth.Models;

/// <summary>The public view of a user — never exposes the password hash.</summary>
public sealed record UserDto
{
    public required int Id { get; init; }

    public required string Email { get; init; }

    public required string Locale { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
