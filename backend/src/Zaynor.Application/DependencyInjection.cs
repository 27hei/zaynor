using Microsoft.Extensions.DependencyInjection;
using Zaynor.Application.Aggregation;

namespace Zaynor.Application;

/// <summary>
/// Registers the Application layer's services. Data sources themselves live in
/// Infrastructure and are registered by <c>AddInfrastructure</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAggregationService, AggregationService>();
        return services;
    }
}
