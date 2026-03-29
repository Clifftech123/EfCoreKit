using EfCoreKit.Abstractions.Models;
using EfCoreKit.BulkOperations.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.MySql.BulkOperations;

/// <summary>
/// MySQL bulk executor using <c>MySqlBulkCopy</c> and <c>ON DUPLICATE KEY UPDATE</c> strategies.
/// </summary>
internal sealed class MySqlBulkExecutor : IBulkExecutor
{
    /// <inheritdoc />
    public Task BulkInsertAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Use MySqlBulkCopy for high-performance insert
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task BulkUpdateAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Insert to temp table + UPDATE with JOIN
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task BulkDeleteAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Insert to temp table + DELETE with JOIN
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task BulkUpsertAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: INSERT ... ON DUPLICATE KEY UPDATE
        throw new NotImplementedException();
    }
}
