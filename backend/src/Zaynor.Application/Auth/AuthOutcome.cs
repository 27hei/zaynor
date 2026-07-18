using Zaynor.Application.Auth.Models;

namespace Zaynor.Application.Auth;

/// <summary>Why an auth operation succeeded or failed, without throwing for expected cases.</summary>
public enum AuthStatus
{
    Success,
    EmailAlreadyExists,
    InvalidCredentials,
}

/// <summary>The result of a register/login attempt.</summary>
public sealed record AuthOutcome
{
    public required AuthStatus Status { get; init; }

    public AuthResponse? Response { get; init; }

    public bool Succeeded => Status == AuthStatus.Success;

    public static AuthOutcome Ok(AuthResponse response) =>
        new() { Status = AuthStatus.Success, Response = response };

    public static AuthOutcome Fail(AuthStatus status) =>
        new() { Status = status };
}
