using EfCoreKit.Interfaces;

namespace EfCoreKit.Entities;

/// <summary>
/// Kitchen-sink base class: audit + soft-delete + optimistic concurrency.
/// Inherit when you want every EfCoreKit feature wired up with zero interface implementation.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class FullEntity<TKey> : SoftDeletableEntity<TKey>, IConcurrencyAware
{
    /// <summary>
    /// Optimistic concurrency token automatically managed by the database.
    /// EF Core raises <see cref="EfCoreKit.Exceptions.ConcurrencyConflictException"/> when
    /// a stale version is detected during update.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// Convenience alias of <see cref="FullEntity{TKey}"/> with an <c>int</c> key.
/// </summary>
public abstract class FullEntity : FullEntity<int> { }
