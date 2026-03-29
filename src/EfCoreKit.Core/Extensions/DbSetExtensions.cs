using EfCoreKit.Abstractions.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="DbSet{T}"/> providing common query shortcuts.
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet.</param>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    public static async Task<T?> GetByIdAsync<T>(
        this DbSet<T> dbSet,
        object id,
        CancellationToken cancellationToken = default) where T : class
    {
        return await dbSet.FindAsync([id], cancellationToken);
    }

    /// <summary>
    /// Gets an entity by its primary key or throws <see cref="EntityNotFoundException"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet.</param>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the entity is not found.</exception>
    public static async Task<T> GetByIdOrThrowAsync<T>(
        this DbSet<T> dbSet,
        object id,
        CancellationToken cancellationToken = default) where T : class
    {
        var entity = await dbSet.FindAsync([id], cancellationToken);
        return entity ?? throw new EntityNotFoundException(typeof(T).Name, id);
    }

    /// <summary>
    /// Checks whether an entity with the given primary key exists.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet.</param>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the entity exists; otherwise, <c>false</c>.</returns>
    public static async Task<bool> ExistsAsync<T>(
        this DbSet<T> dbSet,
        object id,
        CancellationToken cancellationToken = default) where T : class
    {
        var entity = await dbSet.FindAsync([id], cancellationToken);
        return entity is not null;
    }

    /// <summary>
    /// Checks whether any entity matches the given predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if any entity matches; otherwise, <c>false</c>.</returns>
    public static async Task<bool> ExistsAsync<T>(
        this DbSet<T> dbSet,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        return await dbSet.AnyAsync(predicate, cancellationToken);
    }
}
