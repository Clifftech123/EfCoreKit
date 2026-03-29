using EfCoreKit.Abstractions.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.BulkOperations.Abstractions;

/// <summary>
/// Executes bulk operations for a specific database provider.
/// Each supported database provider should have its own implementation.
/// </summary>
public interface IBulkExecutor
{
    /// <summary>
    /// Bulk inserts a collection of entities into the database.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The <see cref="DbContext"/> to use.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="config">Optional bulk operation configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task BulkInsertAsync<T>(
        DbContext context,
        IList<T> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Bulk updates a collection of entities in the database.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The <see cref="DbContext"/> to use.</param>
    /// <param name="entities">The entities to update.</param>
    /// <param name="config">Optional bulk operation configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task BulkUpdateAsync<T>(
        DbContext context,
        IList<T> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Bulk deletes a collection of entities from the database.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The <see cref="DbContext"/> to use.</param>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="config">Optional bulk operation configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task BulkDeleteAsync<T>(
        DbContext context,
        IList<T> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Bulk upserts (insert or update) a collection of entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="context">The <see cref="DbContext"/> to use.</param>
    /// <param name="entities">The entities to upsert.</param>
    /// <param name="config">Optional bulk operation configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task BulkUpsertAsync<T>(
        DbContext context,
        IList<T> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class;
}
