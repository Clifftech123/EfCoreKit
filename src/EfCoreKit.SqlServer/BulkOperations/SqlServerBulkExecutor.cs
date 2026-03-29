using EfCoreKit.Abstractions.Models;
using EfCoreKit.BulkOperations.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.SqlServer.BulkOperations;

/// <summary>
/// SQL Server bulk executor using <c>SqlBulkCopy</c> and temp-table MERGE strategies.
/// </summary>
internal sealed class SqlServerBulkExecutor : IBulkExecutor
{
    /// <inheritdoc />
    public Task BulkInsertAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Use SqlBulkCopy for high-performance insert
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task BulkUpdateAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: BulkInsert to temp table + MERGE for update
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task BulkDeleteAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: BulkInsert to temp table + DELETE with JOIN
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task BulkUpsertAsync<T>(
        DbContext context, IList<T> entities, BulkConfig? config = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: BulkInsert to temp table + MERGE with INSERT/UPDATE
        throw new NotImplementedException();
    }
}
