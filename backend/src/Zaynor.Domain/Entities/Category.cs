namespace Zaynor.Domain.Entities;

/// <summary>
/// A product category, optionally nested under a parent to form a hierarchy
/// (e.g. Electronics → Gaming → Consoles). See spec Section 15.
/// </summary>
public class Category
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Parent category id, or null for a top-level category.</summary>
    public int? ParentCategoryId { get; set; }
}
