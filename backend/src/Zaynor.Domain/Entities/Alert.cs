namespace Zaynor.Domain.Entities;

/// <summary>
/// A price-drop alert a user subscribes to for a product (spec FR8,
/// expansion phase). See spec Section 15.
/// </summary>
public class Alert
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    /// <summary>The condition that triggers the alert, e.g. "price drop" or a target price.</summary>
    public required string TargetCondition { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
