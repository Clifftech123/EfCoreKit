using EfCoreKit.Abstractions.Interfaces;

namespace EfCoreKit.Abstractions.Entities;

/// <summary>
/// Base class with audit + soft-delete fields.
/// Inherit to get <c>IsDeleted</c>, <c>DeletedAt</c>, and <c>DeletedBy</c> automatically
/// managed by EfCoreKit's <c>SoftDeleteInterceptor</c>.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class SoftDeletableEntity<TKey> : AuditableEntity<TKey>, ISoftDeletable
{
    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Convenience alias of <see cref="SoftDeletableEntity{TKey}"/> with an <c>int</c> key.
/// </summary>
public abstract class SoftDeletableEntity : SoftDeletableEntity<int> { }
