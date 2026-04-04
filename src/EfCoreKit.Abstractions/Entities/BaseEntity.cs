namespace EfCoreKit.Abstractions.Entities;

/// <summary>
/// Minimal base class with a typed primary key.
/// Inherit when you don't need audit or soft-delete fields.
/// </summary>
/// <typeparam name="TKey">The primary key type (e.g. <c>int</c>, <c>Guid</c>, <c>string</c>).</typeparam>
public abstract class BaseEntity<TKey>
{
    /// <summary>Gets or sets the primary key.</summary>
    public TKey Id { get; set; } = default!;
}

/// <summary>
/// Convenience alias of <see cref="BaseEntity{TKey}"/> with an <c>int</c> key.
/// </summary>
public abstract class BaseEntity : BaseEntity<int> { }
