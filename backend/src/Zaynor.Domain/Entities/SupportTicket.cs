namespace Zaynor.Domain.Entities;

/// <summary>
/// A customer support conversation. A customer's reply to a closed ticket
/// automatically reopens it (no dead-end support experience) — only the
/// admin explicitly closes.
/// </summary>
public class SupportTicket
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public required string Subject { get; set; }

    public bool IsClosed { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
