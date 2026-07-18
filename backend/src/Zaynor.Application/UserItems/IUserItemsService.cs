using Zaynor.Application.UserItems.Models;

namespace Zaynor.Application.UserItems;

/// <summary>
/// Saved products and price-drop alerts for signed-in users (spec FR8/FR9,
/// expansion phase). Products are found-or-created by their normalized key so
/// ephemeral aggregator results become durable references when a user acts on
/// them. Alert *subscriptions* are stored now; actual notification delivery
/// arrives with background jobs once live price feeds exist.
/// </summary>
public interface IUserItemsService
{
    /// <summary>Saves a product for the user; idempotent (re-saving returns the existing entry).</summary>
    Task<SavedProductDto> SaveProductAsync(int userId, string productName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedProductDto>> GetSavedProductsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Removes a saved product; returns false when it doesn't exist or belongs to another user.</summary>
    Task<bool> RemoveSavedProductAsync(int userId, int savedProductId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a price-drop alert; idempotent per user+product. The current
    /// lowest price (baseline) is recorded in the condition so future
    /// monitoring knows what "drops" means.
    /// </summary>
    Task<AlertDto> CreateAlertAsync(int userId, string productName, decimal? priceBaseline, string? currency, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertDto>> GetAlertsAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> RemoveAlertAsync(int userId, int alertId, CancellationToken cancellationToken = default);
}
