namespace Zaynor.Domain.Entities;

/// <summary>One message within a SupportTicket's thread.</summary>
public class SupportMessage
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int UserId { get; set; }

    public bool IsFromAdmin { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
