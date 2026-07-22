using Microsoft.EntityFrameworkCore;
using Zaynor.Application.Support;
using Zaynor.Application.Support.Models;
using Zaynor.Domain.Entities;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.Support;

/// <summary>Database-backed customer support tickets.</summary>
public sealed class SupportTicketService : ISupportTicketService
{
    private readonly ZaynorDbContext _db;

    public SupportTicketService(ZaynorDbContext db)
    {
        _db = db;
    }

    public async Task<SupportTicketDto> CreateTicketAsync(
        int userId, string subject, string firstMessage, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var ticket = new SupportTicket
        {
            UserId = userId,
            Subject = subject.Trim(),
            IsClosed = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);

        _db.SupportMessages.Add(new SupportMessage
        {
            TicketId = ticket.Id,
            UserId = userId,
            IsFromAdmin = false,
            Body = firstMessage.Trim(),
            CreatedAt = now,
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new SupportTicketDto
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            IsClosed = ticket.IsClosed,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            MessageCount = 1,
        };
    }

    public async Task<IReadOnlyList<SupportTicketDto>> GetMyTicketsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.SupportTickets
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Id)
            .Select(t => new SupportTicketDto
            {
                Id = t.Id,
                Subject = t.Subject,
                IsClosed = t.IsClosed,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                MessageCount = _db.SupportMessages.Count(m => m.TicketId == t.Id),
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SupportTicketDetailDto?> GetMyTicketAsync(int userId, int ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        return new SupportTicketDetailDto
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            IsClosed = ticket.IsClosed,
            CreatedAt = ticket.CreatedAt,
            Messages = await GetMessagesAsync(ticket.Id, cancellationToken),
        };
    }

    public async Task<TicketMessageOutcome> AddMyMessageAsync(
        int userId, int ticketId, string body, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId, cancellationToken);
        if (ticket is null)
        {
            return TicketMessageOutcome.Fail(TicketMessageStatus.NotFound);
        }

        // A customer reply reopens a closed ticket — no dead-end support
        // experience; only the admin explicitly closes.
        ticket.IsClosed = false;
        return await AddMessageAsync(ticket, userId, isFromAdmin: false, body, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminSupportTicketDto>> GetAllTicketsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SupportTickets
            .Join(_db.Users, t => t.UserId, u => u.Id, (t, u) => new AdminSupportTicketDto
            {
                Id = t.Id,
                Subject = t.Subject,
                IsClosed = t.IsClosed,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                MessageCount = _db.SupportMessages.Count(m => m.TicketId == t.Id),
                UserEmail = u.Email,
            })
            .OrderByDescending(dto => dto.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminSupportTicketDetailDto?> GetTicketAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        var result = await _db.SupportTickets
            .Where(t => t.Id == ticketId)
            .Join(_db.Users, t => t.UserId, u => u.Id, (t, u) => new { Ticket = t, u.Email })
            .FirstOrDefaultAsync(cancellationToken);
        if (result is null)
        {
            return null;
        }

        return new AdminSupportTicketDetailDto
        {
            Id = result.Ticket.Id,
            Subject = result.Ticket.Subject,
            IsClosed = result.Ticket.IsClosed,
            CreatedAt = result.Ticket.CreatedAt,
            UserEmail = result.Email,
            Messages = await GetMessagesAsync(result.Ticket.Id, cancellationToken),
        };
    }

    public async Task<TicketMessageOutcome> AddAdminReplyAsync(
        int adminUserId, int ticketId, string body, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketMessageOutcome.Fail(TicketMessageStatus.NotFound);
        }

        // Admin replies are always allowed regardless of closed status.
        return await AddMessageAsync(ticket, adminUserId, isFromAdmin: true, body, cancellationToken);
    }

    public async Task<bool> CloseTicketAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null)
        {
            return false;
        }

        ticket.IsClosed = true;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<TicketMessageOutcome> AddMessageAsync(
        SupportTicket ticket, int userId, bool isFromAdmin, string body, CancellationToken cancellationToken)
    {
        var message = new SupportMessage
        {
            TicketId = ticket.Id,
            UserId = userId,
            IsFromAdmin = isFromAdmin,
            Body = body.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.SupportMessages.Add(message);
        ticket.UpdatedAt = message.CreatedAt;
        await _db.SaveChangesAsync(cancellationToken);

        return TicketMessageOutcome.Ok(new SupportMessageDto
        {
            Id = message.Id,
            IsFromAdmin = message.IsFromAdmin,
            Body = message.Body,
            CreatedAt = message.CreatedAt,
        });
    }

    private async Task<IReadOnlyList<SupportMessageDto>> GetMessagesAsync(int ticketId, CancellationToken cancellationToken)
    {
        return await _db.SupportMessages
            .Where(m => m.TicketId == ticketId)
            .OrderBy(m => m.Id)
            .Select(m => new SupportMessageDto
            {
                Id = m.Id,
                IsFromAdmin = m.IsFromAdmin,
                Body = m.Body,
                CreatedAt = m.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
