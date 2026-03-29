using EfCoreKit.Abstractions.Models;
using EfCoreKit.BulkOperations.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.PostgreSql.BulkOperations;

/// <summary>
/// PostgreSQL bulk executor using <c>COPY BINARY</c> and <c>ON CONFLICT</c> strategies.
/// </summary>
internal sealed class PostgreSqlBulkExecutor : IBulkExecutor
{
    /// <inheritdoc />
    public Task BulkInsertAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Use Npgsql COPY BINARY for high-performance insert
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
        // TODO: INSERT ... ON CONFLICT DO UPDATE
        throw new NotImplementedException();
    }
}
