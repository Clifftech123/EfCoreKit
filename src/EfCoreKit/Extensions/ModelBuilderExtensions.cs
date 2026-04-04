using EfCoreKit.Interfaces;
using EfCoreKit.Filters;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Extensions;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> to configure EfCoreKit conventions.
/// </summary>
/// <remarks>
/// These are additive configurations — all standard <see cref="ModelBuilder"/> APIs
/// (<c>Entity</c>, <c>HasSequence</c>, <c>ApplyConfigurationsFromAssembly</c>, etc.)
/// remain fully available and can be used alongside these helpers.
/// </remarks>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures the concurrency token for all entities implementing <see cref="IConcurrencyAware"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.ApplyConcurrencyTokens();
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder ApplyConcurrencyTokens(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IConcurrencyAware).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(IConcurrencyAware.RowVersion))
                .IsRowVersion();
        }

        return modelBuilder;
    }

    /// <summary>
    /// Configures soft delete global query filters for all entities implementing <see cref="ISoftDeletable"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.ApplySoftDeleteFilters();
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder ApplySoftDeleteFilters(this ModelBuilder modelBuilder)
    {
        SoftDeleteQueryFilter.Apply(modelBuilder);
        return modelBuilder;
    }

    /// <summary>
    /// Configures tenant global query filters for all entities implementing <see cref="ITenantEntity"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.ApplyTenantFilters(_tenantProvider);
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder ApplyTenantFilters(this ModelBuilder modelBuilder, ITenantProvider? tenantProvider)
    {
        TenantQueryFilter.Apply(modelBuilder, tenantProvider);
        return modelBuilder;
    }
}
