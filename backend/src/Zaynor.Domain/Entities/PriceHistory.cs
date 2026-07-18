namespace Zaynor.Domain.Entities;

/// <summary>
/// A recorded price point for a product at a store, accumulated over time to
/// enable future predictive analytics ("buy now or wait?", spec FR12).
/// See spec Section 15.
/// </summary>
public class PriceHistory
{
    public long Id { get; set; }

    public int ProductId { get; set; }

    public int StoreId { get; set; }

    public decimal Price { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
}
