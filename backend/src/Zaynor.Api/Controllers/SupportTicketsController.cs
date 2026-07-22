using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zaynor.Api.Extensions;
using Zaynor.Application.Support;
using Zaynor.Application.Support.Models;

namespace Zaynor.Api.Controllers;

/// <summary>A signed-in customer's own support tickets.</summary>
[ApiController]
[Authorize]
[Route("api/support/tickets")]
public class SupportTicketsController : ControllerBase
{
    private const int MaxSubjectLength = 200;
    private const int MaxMessageLength = 4000;

    public sealed record CreateTicketRequest(string Subject, string Message);

    public sealed record AddMessageRequest(string Body);

    private readonly ISupportTicketService _tickets;

    public SupportTicketsController(ISupportTicketService tickets)
    {
        _tickets = tickets;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupportTicketDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        return Ok(await _tickets.GetMyTicketsAsync(userId, cancellationToken));
    }

    [HttpPost]
    [EnableRateLimiting("submission")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > MaxSubjectLength)
        {
            return BadRequest(new { error = $"subject is required and must be under {MaxSubjectLength} characters." });
        }

        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > MaxMessageLength)
        {
            return BadRequest(new { error = $"message is required and must be under {MaxMessageLength} characters." });
        }

        return Ok(await _tickets.CreateTicketAsync(userId, request.Subject, request.Message, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SupportTicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        var ticket = await _tickets.GetMyTicketAsync(userId, id, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("{id:int}/messages")]
    [EnableRateLimiting("submission")]
    [ProducesResponseType(typeof(SupportMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMessage(int id, [FromBody] AddMessageRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int userId)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > MaxMessageLength)
        {
            return BadRequest(new { error = $"body is required and must be under {MaxMessageLength} characters." });
        }

        var outcome = await _tickets.AddMyMessageAsync(userId, id, request.Body, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Message) : NotFound();
    }
}
