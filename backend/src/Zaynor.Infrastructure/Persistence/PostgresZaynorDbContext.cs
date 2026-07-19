using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zaynor.Infrastructure.Persistence;

/// <summary>
/// The PostgreSQL flavor of the context (spec Section 14's production
/// database). Same model as the base; exists so PG keeps its own migrations
/// under Migrations/Postgres while SQLite keeps the originals. DI registers
/// this type as <see cref="ZaynorDbContext"/> when the connection string is
/// PostgreSQL, so services never know the difference.
/// </summary>
public class PostgresZaynorDbContext : ZaynorDbContext
{
    public PostgresZaynorDbContext(DbContextOptions<PostgresZaynorDbContext> options)
        : base(options)
    {
    }
}

/// <summary>Design-time factory so `dotnet ef` can scaffold PG migrations without a live server.</summary>
public class PostgresZaynorDbContextFactory : IDesignTimeDbContextFactory<PostgresZaynorDbContext>
{
    public PostgresZaynorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostgresZaynorDbContext>()
            .UseNpgsql("Host=localhost;Database=zaynor_design;Username=design;Password=design")
            .Options;

        return new PostgresZaynorDbContext(options);
    }
}

/// <summary>Design-time factory for the SQLite context, so `dotnet ef --context ZaynorDbContext`
/// binds the base context exactly instead of falling through to the Postgres subclass.</summary>
public class ZaynorDbContextFactory : IDesignTimeDbContextFactory<ZaynorDbContext>
{
    public ZaynorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ZaynorDbContext>()
            .UseSqlite("Data Source=zaynor-design.db")
            .Options;

        return new ZaynorDbContext(options);
    }
}
