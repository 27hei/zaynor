namespace Zaynor.Domain.Entities;

/// <summary>
/// A customer's rating + comment about a specific store. Every review is
/// public regardless of rating — hiding negative ones would be a deceptive
/// practice and contradicts the site's honesty-first positioning. The admin
/// can post one public reply to any review instead (like a business owner
/// replying to a Google review).
/// </summary>
public class Review
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public int UserId { get; set; }

    /// <summary>Optional; a blank value renders as a generic label ("Zaynor Customer"/"عميل زينور") client-side rather than exposing the reviewer's real email.</summary>
    public string? DisplayName { get; set; }

    /// <summary>1-5.</summary>
    public int Rating { get; set; }

    public required string Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? AdminReply { get; set; }

    public DateTimeOffset? AdminReplyAt { get; set; }
}
