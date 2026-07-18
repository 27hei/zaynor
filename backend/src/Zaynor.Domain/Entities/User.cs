namespace Zaynor.Domain.Entities;

/// <summary>
/// A registered user. Accounts, saved products, and alerts are an expansion-phase
/// feature (spec FR9); the entity is modeled now so the schema is ready.
/// See spec Section 15.
/// </summary>
public class User
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Preferred locale, e.g. "ar" or "en" (spec NFR5, bilingual support).</summary>
    public string Locale { get; set; } = "ar";
}
