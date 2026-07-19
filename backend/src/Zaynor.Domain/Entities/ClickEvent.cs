namespace Zaynor.Domain.Entities;

/// <summary>
/// One outbound "go to store" click. This is the metric affiliate networks
/// value most ("sends serious buyers to stores", spec Section 20.8) and the
/// pipe affiliate tracking links will flow through once accounts exist.
/// </summary>
public class ClickEvent
{
    public long Id { get; set; }

    public required string StoreName { get; set; }

    public required string ProductName { get; set; }

    public required string Url { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
