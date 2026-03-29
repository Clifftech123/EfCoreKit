using EfCoreKit.BulkOperations.Abstractions;
using EfCoreKit.SqlServer.BulkOperations;
using Microsoft.Extensions.DependencyInjection;

namespace EfCoreKit.SqlServer.Extensions;

/// <summary>
/// Extension methods for registering SQL Server EfCoreKit services.
/// </summary>
public static class SqlServerExtensions
{
    /// <summary>
    /// Adds SQL Server bulk operations support to EfCoreKit.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEfCoreKitSqlServer(this IServiceCollection services)
    {
        services.AddSingleton<IBulkExecutor, SqlServerBulkExecutor>();
        return services;
    }
}
