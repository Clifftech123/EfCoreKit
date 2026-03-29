using EfCoreKit.BulkOperations.Abstractions;
using EfCoreKit.PostgreSql.BulkOperations;
using Microsoft.Extensions.DependencyInjection;

namespace EfCoreKit.PostgreSql.Extensions;

/// <summary>
/// Extension methods for registering PostgreSQL EfCoreKit services.
/// </summary>
public static class PostgreSqlExtensions
{
    /// <summary>
    /// Adds PostgreSQL bulk operations support to EfCoreKit.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEfCoreKitPostgreSql(this IServiceCollection services)
    {
        services.AddSingleton<IBulkExecutor, PostgreSqlBulkExecutor>();
        return services;
    }
}
