using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zaynor.Application.Auth;
using Zaynor.Application.Auth.Models;

namespace Zaynor.Api.Controllers;

/// <summary>Registration, login, and the current-user endpoint (spec FR9).</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var validationError = ValidateCredentials(request.Email, request.Password);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var outcome = await _authService.RegisterAsync(request, cancellationToken);

        return outcome.Status switch
        {
            AuthStatus.Success => Ok(outcome.Response),
            AuthStatus.EmailAlreadyExists => Conflict(new { error = "An account with this email already exists." }),
            _ => BadRequest(new { error = "Registration failed." }),
        };
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var outcome = await _authService.LoginAsync(request, cancellationToken);

        return outcome.Succeeded
            ? Ok(outcome.Response)
            : Unauthorized(new { error = "Invalid email or password." });
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetByIdAsync(userId, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }

    private static string? ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return "A valid email is required.";
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return "Password must be at least 8 characters.";
        }

        return null;
    }
}
