using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Zaynor.Application.Aggregation;
using Zaynor.Application.UserItems;
using Zaynor.Application.UserItems.Models;
using Zaynor.Domain.Entities;
using Zaynor.Infrastructure.Persistence;

namespace Zaynor.Infrastructure.UserItems;

/// <summary>Database-backed saved products and alerts (spec FR8/FR9).</summary>
public sealed class UserItemsService : IUserItemsService
{
    private readonly ZaynorDbContext _db;

    public UserItemsService(ZaynorDbContext db)
    {
        _db = db;
    }

    public async Task<SavedProductDto> SaveProductAsync(int userId, string productName, CancellationToken cancellationToken = default)
    {
        var product = await FindOrCreateProductAsync(productName, cancellationToken);

        var existing = await _db.SavedProducts
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ProductId == product.Id, cancellationToken);

        if (existing is not null)
        {
            return new SavedProductDto { Id = existing.Id, ProductName = product.CanonicalName, SavedAt = existing.SavedAt };
        }

        var saved = new SavedProduct
        {
            UserId = userId,
            ProductId = product.Id,
            SavedAt = DateTimeOffset.UtcNow,
        };

        _db.SavedProducts.Add(saved);
        await _db.SaveChangesAsync(cancellationToken);

        return new SavedProductDto { Id = saved.Id, ProductName = product.CanonicalName, SavedAt = saved.SavedAt };
    }

    public async Task<IReadOnlyList<SavedProductDto>> GetSavedProductsAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Ordered by Id (monotonic) — SQLite cannot ORDER BY DateTimeOffset.
        return await _db.SavedProducts
            .Where(s => s.UserId == userId)
            .Join(_db.Products, s => s.ProductId, p => p.Id, (s, p) => new SavedProductDto
            {
                Id = s.Id,
                ProductName = p.CanonicalName,
                SavedAt = s.SavedAt,
            })
            .OrderByDescending(dto => dto.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RemoveSavedProductAsync(int userId, int savedProductId, CancellationToken cancellationToken = default)
    {
        var saved = await _db.SavedProducts
            .FirstOrDefaultAsync(s => s.Id == savedProductId && s.UserId == userId, cancellationToken);

        if (saved is null)
        {
            return false;
        }

        _db.SavedProducts.Remove(saved);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AlertDto> CreateAlertAsync(int userId, string productName, decimal? priceBaseline, string? currency, CancellationToken cancellationToken = default)
    {
        var product = await FindOrCreateProductAsync(productName, cancellationToken);

        var existing = await _db.Alerts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.ProductId == product.Id && a.IsActive, cancellationToken);

        if (existing is not null)
        {
            return ToAlertDto(existing, product.CanonicalName);
        }

        var alert = new Alert
        {
            UserId = userId,
            ProductId = product.Id,
            TargetCondition = BuildCondition(priceBaseline, currency),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync(cancellationToken);

        return ToAlertDto(alert, product.CanonicalName);
    }

    public async Task<IReadOnlyList<AlertDto>> GetAlertsAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Ordered by Id (monotonic) — SQLite cannot ORDER BY DateTimeOffset.
        return await _db.Alerts
            .Where(a => a.UserId == userId)
            .Join(_db.Products, a => a.ProductId, p => p.Id, (a, p) => new AlertDto
            {
                Id = a.Id,
                ProductName = p.CanonicalName,
                TargetCondition = a.TargetCondition,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
            })
            .OrderByDescending(dto => dto.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RemoveAlertAsync(int userId, int alertId, CancellationToken cancellationToken = default)
    {
        var alert = await _db.Alerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId, cancellationToken);

        if (alert is null)
        {
            return false;
        }

        _db.Alerts.Remove(alert);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Resolves an ephemeral search result to a durable Product row via the
    /// normalized matching key (spec FR3), creating it on first reference.
    /// </summary>
    private async Task<Product> FindOrCreateProductAsync(string productName, CancellationToken cancellationToken)
    {
        var trimmed = productName.Trim();
        var key = ProductNormalizer.Normalize(trimmed);

        var product = await _db.Products.FirstOrDefaultAsync(p => p.NormalizedKey == key, cancellationToken);
        if (product is not null)
        {
            return product;
        }

        product = new Product { CanonicalName = trimmed, NormalizedKey = key };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return product;
    }

    private static string BuildCondition(decimal? priceBaseline, string? currency)
    {
        if (priceBaseline is null)
        {
            return "price_drop";
        }

        var amount = priceBaseline.Value.ToString(CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(currency)
            ? $"price_drop_below:{amount}"
            : $"price_drop_below:{amount} {currency}";
    }

    private static AlertDto ToAlertDto(Alert alert, string productName) => new()
    {
        Id = alert.Id,
        ProductName = productName,
        TargetCondition = alert.TargetCondition,
        IsActive = alert.IsActive,
        CreatedAt = alert.CreatedAt,
    };
}
