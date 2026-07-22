namespace Zaynor.Application.Support.Models;

public sealed record SupportMessageDto
{
    public required int Id { get; init; }

    public required bool IsFromAdmin { get; init; }

    public required string Body { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
