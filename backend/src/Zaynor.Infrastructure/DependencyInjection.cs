using Microsoft.Extensions.DependencyInjection;
using Zaynor.Application.Aggregation;
using Zaynor.Infrastructure.DataSources;

namespace Zaynor.Infrastructure;

/// <summary>
/// Registers Infrastructure-layer services: the concrete data sources the
/// aggregation engine fans out to. Add real feeds/APIs here as additional
/// <see cref="IProductDataSource"/> registrations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IProductDataSource, MockProductDataSource>();
        return services;
    }
}
