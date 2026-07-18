using Zaynor.Application.Auth.Models;

namespace Zaynor.Application.Auth;

/// <summary>
/// Account operations: registration, login, and profile lookup. User accounts
/// are a spec expansion-phase feature (FR9); this brings them online with
/// securely hashed passwords and JWT-based sessions.
/// </summary>
public interface IAuthService
{
    Task<AuthOutcome> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthOutcome> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the user for an authenticated request, or null if not found.</summary>
    Task<UserDto?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
}
