using Zaynor.Application.Support.Models;

namespace Zaynor.Application.Support;

/// <summary>
/// Customer support tickets. A customer's reply to a closed ticket
/// automatically reopens it — only the admin explicitly closes, so there's
/// never a dead end.
/// </summary>
public interface ISupportTicketService
{
    Task<SupportTicketDto> CreateTicketAsync(int userId, string subject, string firstMessage, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportTicketDto>> GetMyTicketsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Null when the ticket doesn't exist or belongs to another user.</summary>
    Task<SupportTicketDetailDto?> GetMyTicketAsync(int userId, int ticketId, CancellationToken cancellationToken = default);

    Task<TicketMessageOutcome> AddMyMessageAsync(int userId, int ticketId, string body, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminSupportTicketDto>> GetAllTicketsAsync(CancellationToken cancellationToken = default);

    Task<AdminSupportTicketDetailDto?> GetTicketAsync(int ticketId, CancellationToken cancellationToken = default);

    Task<TicketMessageOutcome> AddAdminReplyAsync(int adminUserId, int ticketId, string body, CancellationToken cancellationToken = default);

    /// <summary>False when the ticket doesn't exist.</summary>
    Task<bool> CloseTicketAsync(int ticketId, CancellationToken cancellationToken = default);
}
