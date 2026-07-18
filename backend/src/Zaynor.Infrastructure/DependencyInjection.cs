using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Auth;
using Zaynor.Application.UserItems;
using Zaynor.Infrastructure.Aggregation;
using Zaynor.Infrastructure.Auth;
using Zaynor.Infrastructure.DataSources;
using Zaynor.Infrastructure.Persistence;
using Zaynor.Infrastructure.UserItems;

namespace Zaynor.Infrastructure;

/// <summary>
/// Registers Infrastructure-layer services: persistence (EF Core), auth, and
/// the concrete data sources the aggregation engine fans out to.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=zaynor.db";

        services.AddDbContext<ZaynorDbContext>(options => options.UseSqlite(connectionString));

        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var jwtSettings = new JwtSettings
        {
            Issuer = jwtSection["Issuer"] ?? "Zaynor",
            Audience = jwtSection["Audience"] ?? "ZaynorClient",
            Key = jwtSection["Key"] ?? string.Empty,
            ExpiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var minutes) ? minutes : 10080,
        };
        services.AddSingleton(Options.Create(jwtSettings));

        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IUserItemsService, UserItemsService>();

        services.AddScoped<IProductDataSource, MockProductDataSource>();

        // The public engine = core AggregationService (registered by
        // AddApplication) decorated with caching + price-history recording
        // (spec Section 13).
        services.AddMemoryCache();
        services.AddScoped<IPriceHistoryRecorder, PriceHistoryRecorder>();
        services.AddScoped<IAggregationService, CachedAggregationService>();
        services.AddScoped<ISearchSuggestionService, SearchSuggestionService>();

        return services;
    }
}
