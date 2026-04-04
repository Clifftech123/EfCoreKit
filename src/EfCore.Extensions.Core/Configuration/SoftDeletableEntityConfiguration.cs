using EfCore.Extensions.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Extensions.Core.Configuration;

/// <summary>
/// EF Core entity configuration base for <see cref="SoftDeletableEntity{TKey}"/>.
/// Automatically configures soft-delete columns, a default value of <c>false</c> on
/// <c>IsDeleted</c>, and a composite index on <c>(IsDeleted, CreatedAt)</c>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class SoftDeletableEntityConfiguration<TEntity, TKey> : AuditableEntityConfiguration<TEntity, TKey>
    where TEntity : SoftDeletableEntity<TKey>
{
    /// <inheritdoc />
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DeletedBy).HasMaxLength(256);

        // Composite index — most soft-delete queries filter both IsDeleted and a date column
        builder.HasIndex(e => new { e.IsDeleted, e.CreatedAt });

        ConfigureSoftDeletableEntity(builder);
    }

    /// <summary>Override to add entity-specific configuration on top of soft-delete defaults.</summary>
    protected virtual void ConfigureSoftDeletableEntity(EntityTypeBuilder<TEntity> builder) { }
}
