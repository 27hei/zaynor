namespace Zaynor.Application.Support.Models;

/// <summary>A ticket summary for a customer's own ticket list.</summary>
public sealed record SupportTicketDto
{
    public required int Id { get; init; }

    public required string Subject { get; init; }

    public required bool IsClosed { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required int MessageCount { get; init; }
}

/// <summary>A ticket's full thread for the customer who owns it.</summary>
public sealed record SupportTicketDetailDto
{
    public required int Id { get; init; }

    public required string Subject { get; init; }

    public required bool IsClosed { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required IReadOnlyList<SupportMessageDto> Messages { get; init; }
}

/// <summary>A ticket summary for the admin inbox — carries the customer's email since the admin manages tickets across every user.</summary>
public sealed record AdminSupportTicketDto
{
    public required int Id { get; init; }

    public required string Subject { get; init; }

    public required bool IsClosed { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public required int MessageCount { get; init; }

    public required string UserEmail { get; init; }
}

/// <summary>A ticket's full thread for the admin.</summary>
public sealed record AdminSupportTicketDetailDto
{
    public required int Id { get; init; }

    public required string Subject { get; init; }

    public required bool IsClosed { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required string UserEmail { get; init; }

    public required IReadOnlyList<SupportMessageDto> Messages { get; init; }
}
