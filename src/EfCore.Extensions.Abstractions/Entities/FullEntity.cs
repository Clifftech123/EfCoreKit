using EfCore.Extensions.Abstractions.Interfaces;

namespace EfCore.Extensions.Abstractions.Entities;

/// <summary>
/// Kitchen-sink base class: audit + soft-delete + multi-tenancy + optimistic concurrency.
/// Inherit when you want every EfCore.Extensions feature wired up with zero interface implementation.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class FullEntity<TKey> : SoftDeletableEntity<TKey>, ITenantEntity, IConcurrencyAware
{
    /// <inheritdoc />
    public string? TenantId { get; set; }

    /// <summary>
    /// Optimistic concurrency token automatically managed by the database.
    /// EF Core raises <see cref="EfCore.Extensions.Abstractions.Exceptions.ConcurrencyConflictException"/> when
    /// a stale version is detected during update.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// Convenience alias of <see cref="FullEntity{TKey}"/> with an <c>int</c> key.
/// </summary>
public abstract class FullEntity : FullEntity<int> { }
