using System.Linq.Expressions;
using EfCoreKit.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Filters;

/// <summary>
/// Applies a global query filter that automatically excludes soft-deleted entities
/// (where <see cref="ISoftDeletable.IsDeleted"/> is <c>true</c>).
/// </summary>
/// <remarks>
/// Iterates all entity types in the model that implement <see cref="ISoftDeletable"/>
/// and adds <c>HasQueryFilter(e =&gt; e.IsDeleted == false)</c> via expression trees.
/// To include soft-deleted entities in a specific query, use <c>.IgnoreQueryFilters()</c>.
/// </remarks>
/// <example>
/// Typically called from <c>OnModelCreating</c>:
/// <code>
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     base.OnModelCreating(modelBuilder);
///     SoftDeleteQueryFilter.Apply(modelBuilder);
/// }
///
/// // Query soft-deleted entities explicitly:
/// var archived = await db.Orders.IgnoreQueryFilters()
///     .Where(o =&gt; o.IsDeleted)
///     .ToListAsync();
/// </code>
/// </example>
internal static class SoftDeleteQueryFilter
{
    /// <summary>
    /// Configures the soft delete global filter for all entity types implementing <see cref="ISoftDeletable"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
