namespace Zaynor.Domain.Entities;

/// <summary>
/// A canonical product that offers from different stores are matched against.
/// <see cref="NormalizedKey"/> powers product matching (spec FR3): different
/// store listings that resolve to the same key are treated as the same product.
/// See spec Section 15.
/// </summary>
public class Product
{
    public int Id { get; set; }

    public required string CanonicalName { get; set; }

    public int? CategoryId { get; set; }

    public string? Brand { get; set; }

    public string? Model { get; set; }

    /// <summary>Normalized matching key derived from the product name (spec FR3).</summary>
    public required string NormalizedKey { get; set; }

    public string? ImageUrl { get; set; }
}
