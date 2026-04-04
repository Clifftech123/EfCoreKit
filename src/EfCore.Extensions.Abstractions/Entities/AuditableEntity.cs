using EfCore.Extensions.Abstractions.Interfaces;

namespace EfCore.Extensions.Abstractions.Entities;

/// <summary>
/// Base class with audit fields automatically populated by EfCore.Extensions's
/// <c>AuditInterceptor</c> during <c>SaveChangesAsync</c>.
/// Inherit to get <c>CreatedAt</c>, <c>CreatedBy</c>, <c>UpdatedAt</c>,
/// and <c>UpdatedBy</c> for free — no manual implementation needed.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class AuditableEntity<TKey> : BaseEntity<TKey>, IAuditable
{
    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc />
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Convenience alias of <see cref="AuditableEntity{TKey}"/> with an <c>int</c> key.
/// </summary>
public abstract class AuditableEntity : AuditableEntity<int> { }
