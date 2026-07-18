namespace Zaynor.Domain.Entities;

/// <summary>
/// A single store's offer for a product: its price and the outbound
/// (affiliate tracking) link. A product has many offers, one per store.
/// See spec Section 15.
/// </summary>
public class Offer
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int StoreId { get; set; }

    public decimal Price { get; set; }

    public required string Currency { get; set; }

    /// <summary>Outbound URL to the store — this is the affiliate tracking link (spec FR7).</summary>
    public required string ProductUrl { get; set; }

    public bool InStock { get; set; } = true;

    public string? ShippingInfo { get; set; }

    public DateTimeOffset LastUpdated { get; set; }
}
