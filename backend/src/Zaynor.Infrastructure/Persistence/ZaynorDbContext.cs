using Microsoft.EntityFrameworkCore;
using Zaynor.Domain.Entities;

namespace Zaynor.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context for Zaynor. Currently backed by SQLite for
/// local development (a single file, zero setup); the provider is chosen in
/// <c>AddInfrastructure</c> and can be swapped to PostgreSQL/SQL Server for
/// production without changing this class (spec Section 14).
/// </summary>
public class ZaynorDbContext : DbContext
{
    public ZaynorDbContext(DbContextOptions<ZaynorDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<SavedProduct> SavedProducts => Set<SavedProduct>();
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Email is the login identity — must be unique.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // A product's normalized key is the basis for cross-store matching.
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.NormalizedKey);

        // Money: keep a sane precision for providers that honor it.
        modelBuilder.Entity<Offer>()
            .Property(o => o.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PriceHistory>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);
    }
}
