using EfCoreKit.BulkOperations.Abstractions;
using EfCoreKit.MySql.BulkOperations;
using Microsoft.Extensions.DependencyInjection;

namespace EfCoreKit.MySql.Extensions;

/// <summary>
/// Extension methods for registering MySQL EfCoreKit services.
/// </summary>
public static class MySqlExtensions
{
    /// <summary>
    /// Adds MySQL bulk operations support to EfCoreKit.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEfCoreKitMySql(this IServiceCollection services)
    {
        services.AddSingleton<IBulkExecutor, MySqlBulkExecutor>();
        return services;
    }
}
