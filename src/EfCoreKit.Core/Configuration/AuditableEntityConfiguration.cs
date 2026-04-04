using EfCoreKit.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreKit.Core.Configuration;

/// <summary>
/// EF Core entity configuration base for <see cref="AuditableEntity{TKey}"/>.
/// Automatically configures audit columns and an index on <c>CreatedAt</c>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class AuditableEntityConfiguration<TEntity, TKey> : BaseEntityConfiguration<TEntity, TKey>
    where TEntity : AuditableEntity<TKey>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(256);
        builder.Property(e => e.UpdatedBy).HasMaxLength(256);

        builder.HasIndex(e => e.CreatedAt);

        ConfigureAuditableEntity(builder);
    }

    /// <summary>Override to add entity-specific configuration on top of audit defaults.</summary>
    protected virtual void ConfigureAuditableEntity(EntityTypeBuilder<TEntity> builder) { }
}
