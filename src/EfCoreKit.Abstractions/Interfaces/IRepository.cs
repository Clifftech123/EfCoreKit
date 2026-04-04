using System.Linq.Expressions;

namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Full CRUD repository contract.
/// Extends <see cref="IReadRepository{T}"/> with write operations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> : IReadRepository<T> where T : class
{
    /// <summary>Stages a new entity for insertion. Persisted on next <c>SaveChangesAsync</c>.</summary>
    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>Stages multiple new entities for insertion.</summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    /// <summary>Marks an entity as modified. Persisted on next <c>SaveChangesAsync</c>.</summary>
    void Update(T entity);

    /// <summary>Marks multiple entities as modified.</summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>Marks an entity for deletion. Persisted on next <c>SaveChangesAsync</c>.</summary>
    void Remove(T entity);

    /// <summary>Loads and marks the entity with the given key for deletion.</summary>
    Task RemoveByIdAsync(object id, CancellationToken ct = default);

    /// <summary>Loads and marks all entities matching <paramref name="predicate"/> for deletion.</summary>
    Task RemoveRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Persists all staged changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
