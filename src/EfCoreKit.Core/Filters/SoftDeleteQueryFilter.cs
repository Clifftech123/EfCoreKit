using EfCoreKit.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Filters;

/// <summary>
/// Applies a global query filter that automatically excludes soft-deleted entities
/// (where <see cref="ISoftDeletable.IsDeleted"/> is <c>true</c>).
/// </summary>
internal static class SoftDeleteQueryFilter
{
    /// <summary>
    /// Configures the soft delete global filter for all entity types implementing <see cref="ISoftDeletable"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        // TODO: Iterate model entity types
        // - For each type assignable to ISoftDeletable, build expression: e => !e.IsDeleted
        // - Apply via modelBuilder.Entity(type).HasQueryFilter(lambda)
    }
}
