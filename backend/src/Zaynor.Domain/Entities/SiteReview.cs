namespace Zaynor.Domain.Entities;

/// <summary>
/// A customer's rating + comment about Zaynor itself (the platform/idea/
/// experience) — distinct from Review, which rates a specific external
/// store. Shown publicly on the homepage. Unlike store reviews (never
/// hidden, admin may only reply), the admin can delete a site review here —
/// this is moderation of content about the founder's own platform, not
/// suppression of legitimate feedback about a third-party store a shopper
/// is deciding whether to trust with their money.
/// </summary>
public class SiteReview
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>Optional; a blank value renders as a generic label client-side rather than exposing the reviewer's real email.</summary>
    public string? DisplayName { get; set; }

    /// <summary>1-5.</summary>
    public int Rating { get; set; }

    public required string Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
