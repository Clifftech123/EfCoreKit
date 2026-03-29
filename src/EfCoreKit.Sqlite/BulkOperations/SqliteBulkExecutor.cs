using EfCoreKit.Abstractions.Models;
using EfCoreKit.BulkOperations.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Sqlite.BulkOperations;

/// <summary>
/// SQLite bulk executor using batched inserts (SQLite has no native bulk copy).
/// </summary>
internal sealed class SqliteBulkExecutor : IBulkExecutor
{
    /// <inheritdoc />
    public Task BulkInsertAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Batched INSERT using parameterized statements
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task BulkUpdateAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Batched UPDATE statements
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task BulkDeleteAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Batched DELETE statements
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task BulkUpsertAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: INSERT OR REPLACE
        throw new NotImplementedException();
    }
}
