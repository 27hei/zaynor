using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zaynor.Api.Extensions;
using Zaynor.Application.Support;
using Zaynor.Application.Support.Models;

namespace Zaynor.Api.Controllers;

/// <summary>Admin-only support inbox: every ticket across every user.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/support/tickets")]
public class AdminSupportController : ControllerBase
{
    private const int MaxMessageLength = 4000;

    public sealed record AddMessageRequest(string Body);

    private readonly ISupportTicketService _tickets;

    public AdminSupportController(ISupportTicketService tickets)
    {
        _tickets = tickets;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminSupportTicketDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _tickets.GetAllTicketsAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AdminSupportTicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetTicketAsync(id, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("{id:int}/messages")]
    [ProducesResponseType(typeof(SupportMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reply(int id, [FromBody] AddMessageRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not int adminUserId)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > MaxMessageLength)
        {
            return BadRequest(new { error = $"body is required and must be under {MaxMessageLength} characters." });
        }

        var outcome = await _tickets.AddAdminReplyAsync(adminUserId, id, request.Body, cancellationToken);
        return outcome.Succeeded ? Ok(outcome.Message) : NotFound();
    }

    [HttpPost("{id:int}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(int id, CancellationToken cancellationToken)
    {
        var closed = await _tickets.CloseTicketAsync(id, cancellationToken);
        return closed ? NoContent() : NotFound();
    }
}
