using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zaynor.Application.Aggregation;
using Zaynor.Application.Auth;
using Zaynor.Application.ImageSearch;
using Zaynor.Application.Reviews;
using Zaynor.Application.SiteReviews;
using Zaynor.Application.Support;
using Zaynor.Application.UserItems;
using Zaynor.Infrastructure.Aggregation;
using Zaynor.Infrastructure.Alerts;
using Zaynor.Infrastructure.Auth;
using Zaynor.Infrastructure.DataSources;
using Zaynor.Infrastructure.ImageSearch;
using Zaynor.Infrastructure.Persistence;
using Zaynor.Infrastructure.Reviews;
using Zaynor.Infrastructure.SiteReviews;
using Zaynor.Infrastructure.Support;
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
        // Provider by connection string: PostgreSQL for production scale
        // (spec Section 14), SQLite for zero-setup local dev.
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=zaynor.db";
        var isPostgres = connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase);

        if (isPostgres)
        {
            services.AddDbContext<ZaynorDbContext, PostgresZaynorDbContext>(
                options => options.UseNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<ZaynorDbContext>(options => options.UseSqlite(connectionString));
        }

        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var jwtSettings = new JwtSettings
        {
            Issuer = jwtSection["Issuer"] ?? "Zaynor",
            Audience = jwtSection["Audience"] ?? "ZaynorClient",
            Key = jwtSection["Key"] ?? string.Empty,
            ExpiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var minutes) ? minutes : 10080,
        };
        services.AddSingleton(Options.Create(jwtSettings));

        // Which affiliate mechanisms are actually configured right now —
        // the same config keys OutController reads to decide whether to tag
        // an outbound link, bound once here so the aggregation engine can
        // flag which offers currently support Zaynor without ever seeing
        // the actual tag/template secrets themselves.
        var affiliateSection = configuration.GetSection("Affiliate");
        services.AddSingleton(new AffiliateSettings
        {
            AmazonTagConfigured = !string.IsNullOrWhiteSpace(affiliateSection["AmazonTag"]),
            NoonSuffixConfigured = !string.IsNullOrWhiteSpace(affiliateSection["NoonUtmSuffix"]),
            DeeplinkConfigured = affiliateSection["DeeplinkTemplate"] is { Length: > 0 } template && template.Contains("{url}"),
            DeeplinkHosts = (affiliateSection["DeeplinkHosts"] ?? "jarir.com,extra.com,aliexpress.com")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IUserItemsService, UserItemsService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ISupportTicketService, SupportTicketService>();
        services.AddScoped<ISiteReviewService, SiteReviewService>();

        // Real curated catalog first (spec 9.4 manual entry); mock is the
        // flagged fallback for uncovered queries.
        services.AddSingleton<CuratedProductDataSource>();
        services.AddSingleton<IProductDataSource>(sp => sp.GetRequiredService<CuratedProductDataSource>());

        // Live external feeds (real prices/images/links at catalogue scale).
        // Each is dormant until its API key is configured, so registering them
        // unconditionally changes nothing until a key exists (config-only
        // activation). HttpClient is needed for their outbound calls.
        //
        // A short per-client timeout matters for perceived search speed: all
        // sources run concurrently (AggregationService.QueryAllAsync), so the
        // whole search is only ever as fast as its slowest source. Without
        // this, one hung upstream API would silently hold every search to
        // .NET's 100s HttpClient default instead of a few seconds.
        var liveSourceTimeout = TimeSpan.FromSeconds(8);
        services.AddHttpClient();
        services.AddHttpClient(nameof(RainforestAmazonDataSource), c => c.Timeout = liveSourceTimeout);
        services.AddHttpClient(nameof(AliExpressProductDataSource), c => c.Timeout = liveSourceTimeout);
        // GoogleShoppingDataSource (SerpApi) makes a base call plus several
        // parallel per-product expansions to resolve real per-merchant
        // links (see its own remarks) — a longer timeout than the other
        // single-call sources so that round trip isn't cut short.
        services.AddHttpClient(nameof(GoogleShoppingDataSource), c => c.Timeout = TimeSpan.FromSeconds(15));
        // DataForSEO's Amazon live/advanced endpoint routinely takes ~17s
        // (their own documented example), so it needs the same longer budget
        // as GoogleShoppingDataSource rather than the default 8s.
        services.AddHttpClient(nameof(DataForSeoAmazonDataSource), c => c.Timeout = TimeSpan.FromSeconds(20));
        services.AddScoped<IProductDataSource, RainforestAmazonDataSource>();
        services.AddScoped<IProductDataSource, AliExpressProductDataSource>();
        services.AddScoped<IProductDataSource, GoogleShoppingDataSource>();
        services.AddScoped<IProductDataSource, DataForSeoAmazonDataSource>();

        // "Search by photo" — Serper's reverse-image (Lens) endpoint. Its own
        // account/key (DataSources:Serper:ApiKey), separate from
        // GoogleShoppingDataSource's SerpApi key above.
        services.AddSingleton<ITempImageStore, MemoryTempImageStore>();
        services.AddHttpClient(nameof(SerperLensQueryResolver), c => c.Timeout = liveSourceTimeout);
        services.AddScoped<IImageQueryResolver, SerperLensQueryResolver>();

        // The public engine = core AggregationService (registered by
        // AddApplication) decorated with caching + price-history recording
        // (spec Section 13).
        services.AddMemoryCache();
        services.AddScoped<IPriceHistoryRecorder, PriceHistoryRecorder>();
        services.AddScoped<IAggregationService, CachedAggregationService>();
        services.AddScoped<ISearchSuggestionService, SearchSuggestionService>();
        services.AddScoped<IPriceHistoryService, PriceHistoryService>();

        // Spec Section 13 background job: periodic checks that fire alerts
        // and keep history accumulating for tracked products.
        services.AddHostedService<AlertMonitorService>();

        return services;
    }
}
