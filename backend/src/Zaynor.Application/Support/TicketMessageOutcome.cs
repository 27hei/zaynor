using Zaynor.Application.Support.Models;

namespace Zaynor.Application.Support;

public enum TicketMessageStatus
{
    Success,
    NotFound,
}

/// <summary>The result of adding a message to a ticket, without throwing for expected cases.</summary>
public sealed record TicketMessageOutcome
{
    public required TicketMessageStatus Status { get; init; }

    public SupportMessageDto? Message { get; init; }

    public bool Succeeded => Status == TicketMessageStatus.Success;

    public static TicketMessageOutcome Ok(SupportMessageDto message) =>
        new() { Status = TicketMessageStatus.Success, Message = message };

    public static TicketMessageOutcome Fail(TicketMessageStatus status) =>
        new() { Status = status };
}
