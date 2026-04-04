using System.Linq.Expressions;
using EfCoreKit.Models;

namespace EfCoreKit.Interfaces;

/// <summary>
/// Read-only repository contract.
/// Use when a service layer component only needs to query data — no writes.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IReadRepository<T> where T : class
{
    /// <summary>Returns the entity with the given key, or <c>null</c>.</summary>
    Task<T?> GetByIdAsync(object id, CancellationToken ct = default);

    /// <summary>Returns the entity with the given key, or throws <see cref="EfCoreKit.Exceptions.EntityNotFoundException"/>.</summary>
    Task<T> GetByIdOrThrowAsync(object id, CancellationToken ct = default);

    /// <summary>Returns all entities as a read-only list. Avoid on large tables — prefer filtered queries.</summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all entities matching <paramref name="predicate"/>.</summary>
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Applies <paramref name="specification"/> and returns matching entities.</summary>
    Task<IReadOnlyList<T>> FindAsync(ISpecification<T> specification, CancellationToken ct = default);

    /// <summary>Returns the first entity matching <paramref name="predicate"/>, or <c>null</c>.</summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Returns <c>true</c> if an entity with the given key exists.</summary>
    Task<bool> ExistsAsync(object id, CancellationToken ct = default);

    /// <summary>Returns <c>true</c> if any entity matches <paramref name="predicate"/>.</summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Returns the count of entities, optionally filtered.</summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>Returns a paged result using offset-based pagination.</summary>
    Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
}
