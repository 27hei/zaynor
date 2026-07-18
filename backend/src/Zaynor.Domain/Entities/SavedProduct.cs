namespace Zaynor.Domain.Entities;

/// <summary>
/// A product a user has saved to their list (spec FR9, expansion phase).
/// See spec Section 15.
/// </summary>
public class SavedProduct
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public DateTimeOffset SavedAt { get; set; }
}
