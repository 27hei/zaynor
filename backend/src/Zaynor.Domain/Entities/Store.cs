namespace Zaynor.Domain.Entities;

/// <summary>
/// A store Zaynor aggregates offers from (e.g. Amazon, Noon). The affiliate
/// network is the source of both the price feed and the tracking link.
/// See spec Section 15.
/// </summary>
public class Store
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? LogoUrl { get; set; }

    public required string BaseUrl { get; set; }

    /// <summary>The affiliate network this store's offers/links come through (e.g. ArabClicks).</summary>
    public string? AffiliateNetwork { get; set; }

    public bool IsActive { get; set; } = true;
}
