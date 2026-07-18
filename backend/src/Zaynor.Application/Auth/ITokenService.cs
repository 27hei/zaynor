using Zaynor.Domain.Entities;

namespace Zaynor.Application.Auth;

/// <summary>Issues signed JWT access tokens for authenticated users.</summary>
public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateToken(User user);
}
