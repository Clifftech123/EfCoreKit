using System.Linq.Expressions;
using EfCore.Extensions.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Extensions.Core.Filters;

/// <summary>
/// Applies a global query filter that automatically scopes queries to the current tenant
/// for entities implementing <see cref="ITenantEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// Iterates all entity types implementing <see cref="ITenantEntity"/> and adds
/// <c>HasQueryFilter(e =&gt; e.TenantId == tenantProvider.GetCurrentTenantId())</c>.
/// </para>
/// <para>
/// The <see cref="ITenantProvider.GetCurrentTenantId"/> call is captured as an expression
/// and evaluated at query execution time — not at model build time — so the filter
/// always reflects the current tenant context.
/// </para>
/// </remarks>
/// <example>
/// Typically called from <c>OnModelCreating</c>:
/// <code>
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     base.OnModelCreating(modelBuilder);
///     TenantQueryFilter.Apply(modelBuilder, _tenantProvider);
/// }
///
/// // Bypass tenant filter for admin queries:
/// var all = await db.Orders.IgnoreQueryFilters().ToListAsync();
/// </code>
/// </example>
internal static class TenantQueryFilter
{
    /// <summary>
    /// Configures the tenant global filter for all entity types implementing <see cref="ITenantEntity"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="tenantProvider">The tenant provider for resolving the current tenant at query time.</param>
    public static void Apply(ModelBuilder modelBuilder, ITenantProvider? tenantProvider)
    {
        if (tenantProvider is null)
            return;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));

            // Call tenantProvider.GetCurrentTenantId() at query execution time (not model build time)
            var providerExpr = Expression.Constant(tenantProvider);
            var getCurrentTenantCall = Expression.Call(
                providerExpr,
                typeof(ITenantProvider).GetMethod(nameof(ITenantProvider.GetCurrentTenantId))!);

            var condition = Expression.Equal(tenantProperty, getCurrentTenantCall);
            var lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
