using EfCoreKit.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Filters;

/// <summary>
/// Applies a global query filter that automatically scopes queries to the current tenant
/// for entities implementing <see cref="ITenantEntity"/>.
/// </summary>
internal static class TenantQueryFilter
{
    /// <summary>
    /// Configures the tenant global filter for all entity types implementing <see cref="ITenantEntity"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="tenantProvider">The tenant provider for resolving the current tenant at query time.</param>
    public static void Apply(ModelBuilder modelBuilder, ITenantProvider? tenantProvider)
    {
        // TODO: Iterate model entity types
        // - For each type assignable to ITenantEntity, build expression: e => e.TenantId == currentTenantId
        // - Apply via modelBuilder.Entity(type).HasQueryFilter(lambda)
    }
}
