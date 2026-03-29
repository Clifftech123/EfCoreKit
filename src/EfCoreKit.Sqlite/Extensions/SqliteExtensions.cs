using EfCoreKit.BulkOperations.Abstractions;
using EfCoreKit.Sqlite.BulkOperations;
using Microsoft.Extensions.DependencyInjection;

namespace EfCoreKit.Sqlite.Extensions;

/// <summary>
/// Extension methods for registering SQLite EfCoreKit services.
/// </summary>
public static class SqliteExtensions
{
    /// <summary>
    /// Adds SQLite bulk operations support to EfCoreKit.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEfCoreKitSqlite(this IServiceCollection services)
    {
        services.AddSingleton<IBulkExecutor, SqliteBulkExecutor>();
        return services;
    }
}
