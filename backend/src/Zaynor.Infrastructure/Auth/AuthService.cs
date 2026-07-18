using Microsoft.EntityFrameworkCore;
using Zaynor.Application.Auth;
using Zaynor.Application.Auth.Models;
using Zaynor.Domain.Entities;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.Auth;

/// <summary>
/// Account operations backed by the database. Passwords are hashed with BCrypt
/// (never stored in plain text, spec NFR3); a successful register/login returns
/// a signed JWT for the session.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly ZaynorDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthService(ZaynorDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<AuthOutcome> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (emailTaken)
        {
            return AuthOutcome.Fail(AuthStatus.EmailAlreadyExists);
        }

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTimeOffset.UtcNow,
            Locale = NormalizeLocale(request.Locale),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return AuthOutcome.Ok(BuildResponse(user));
    }

    public async Task<AuthOutcome> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Verify even when the user is missing would be ideal to avoid timing
        // leaks; for this stage a straightforward check is acceptable.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return AuthOutcome.Fail(AuthStatus.InvalidCredentials);
        }

        return AuthOutcome.Ok(BuildResponse(user));
    }

    public async Task<UserDto?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken);
        return user is null ? null : ToDto(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expiresAt) = _tokenService.GenerateToken(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = ToDto(user),
        };
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Locale = user.Locale,
        CreatedAt = user.CreatedAt,
    };

    private static string NormalizeLocale(string locale) =>
        locale is "en" or "ar" ? locale : "ar";
}
