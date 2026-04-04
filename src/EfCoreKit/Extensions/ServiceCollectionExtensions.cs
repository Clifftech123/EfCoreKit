using EfCoreKit.Interfaces;
using EfCoreKit.Context;
using EfCoreKit.Interceptors;
using EfCoreKit.Repositories;
using EfCoreKit.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfCoreKit.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register EfCoreKit services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds EfCoreKit services and configures the specified <typeparamref name="TContext"/>.
    /// </summary>
    /// <typeparam name="TContext">The application DbContext type derived from <see cref="EfCoreDbContext{TContext}"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">The EF Core DbContext options configuration.</param>
    /// <param name="configureOptions">Optional EfCoreKit feature configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddEfCoreExtensions&lt;AppDbContext&gt;(
    ///     options => options.UseSqlServer(connectionString),
    ///     kit => kit
    ///         .EnableSoftDelete()
    ///         .EnableAuditTrail()
    ///         .EnableMultiTenancy()
    ///         .UseUserProvider&lt;HttpContextUserProvider&gt;()
    ///         .UseTenantProvider&lt;HttpContextTenantProvider&gt;()
    ///         .LogSlowQueries(TimeSpan.FromSeconds(1)));
    /// </code>
    /// </example>
    public static IServiceCollection AddEfCoreExtensions<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction,
        Action<EfCoreOptions>? configureOptions = null)
        where TContext : EfCoreDbContext<TContext>
    {
        // Build options
        var kitOptions = new EfCoreOptions();
        configureOptions?.Invoke(kitOptions);
        services.AddSingleton(kitOptions);

        // Register user provider if configured
        if (kitOptions.UserProviderType is not null)
        {
            services.AddScoped(typeof(IUserProvider), kitOptions.UserProviderType);
        }

        // Register tenant provider if configured
        if (kitOptions.TenantProviderType is not null)
        {
            services.AddScoped(typeof(ITenantProvider), kitOptions.TenantProviderType);
        }

        // Register interceptors based on enabled features
        if (kitOptions.AuditTrailEnabled)
        {
            services.AddScoped<AuditInterceptor>();
        }

        if (kitOptions.SoftDeleteEnabled)
        {
            services.AddScoped<SoftDeleteInterceptor>();
        }

        if (kitOptions.MultiTenancyEnabled)
        {
            services.AddScoped<TenantInterceptor>();
        }

        if (kitOptions.SlowQueryThreshold is not null)
        {
            services.AddScoped<SlowQueryInterceptor>();
        }

        // Register generic repository and unit-of-work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();

        // Register the DbContext with interceptors wired in
        services.AddDbContext<TContext>((sp, dbOptions) =>
        {
            optionsAction(dbOptions);

            if (kitOptions.AuditTrailEnabled)
            {
                dbOptions.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            }

            if (kitOptions.SoftDeleteEnabled)
            {
                dbOptions.AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>());
            }

            if (kitOptions.MultiTenancyEnabled)
            {
                dbOptions.AddInterceptors(sp.GetRequiredService<TenantInterceptor>());
            }

            if (kitOptions.SlowQueryThreshold is not null)
            {
                dbOptions.AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>());
            }

        });

        return services;
    }
}
