using Microsoft.Extensions.DependencyInjection;
using Zaynor.Application.Aggregation;

namespace Zaynor.Application;

/// <summary>
/// Registers the Application layer's services. The core engine is registered
/// as its concrete type; Infrastructure decorates it (caching + price-history
/// recording, spec Section 13) and binds <see cref="IAggregationService"/>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AggregationService>();
        return services;
    }
}
