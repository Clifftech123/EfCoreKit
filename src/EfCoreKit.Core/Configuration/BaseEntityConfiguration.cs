using EfCoreKit.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreKit.Core.Configuration;

/// <summary>
/// EF Core entity configuration base for <see cref="BaseEntity{TKey}"/>.
/// Inherit instead of implementing <see cref="IEntityTypeConfiguration{TEntity}"/> directly
/// to get a primary key configured automatically.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class BaseEntityConfiguration<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity<TKey>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        ConfigureEntity(builder);
    }

    /// <summary>Override to add entity-specific configuration.</summary>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
