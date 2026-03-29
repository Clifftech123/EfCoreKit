using EfCoreKit.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> to configure EfCoreKit conventions.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures the concurrency token for all entities implementing <see cref="IConcurrencyAware"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyConcurrencyTokens(this ModelBuilder modelBuilder)
    {
        // TODO: Iterate entity types implementing IConcurrencyAware
        // - Configure RowVersion as a concurrency token / row version
        return modelBuilder;
    }

    /// <summary>
    /// Configures soft delete global query filters for all entities implementing <see cref="ISoftDeletable"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplySoftDeleteFilters(this ModelBuilder modelBuilder)
    {
        // TODO: Delegate to SoftDeleteQueryFilter.Apply
        return modelBuilder;
    }

    /// <summary>
    /// Configures tenant global query filters for all entities implementing <see cref="ITenantEntity"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyTenantFilters(this ModelBuilder modelBuilder, ITenantProvider? tenantProvider)
    {
        // TODO: Delegate to TenantQueryFilter.Apply
        return modelBuilder;
    }
}
