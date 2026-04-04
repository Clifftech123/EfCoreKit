using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Extensions;

/// <summary>
/// Utility extension methods for <see cref="DbContext"/>.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Executes <paramref name="action"/> inside a database transaction.
    /// Commits on success, rolls back and re-throws on any exception.
    /// Respects EF Core execution strategies (e.g. SQL Server retry-on-transient-failure).
    /// </summary>
    /// <example>
    /// <code>
    /// await context.ExecuteInTransactionAsync(async () =>
    /// {
    ///     context.Orders.Add(order);
    ///     context.Inventory.Update(stock);
    ///     await context.SaveChangesAsync();
    /// });
    /// </code>
    /// </example>
    public static async Task ExecuteInTransactionAsync(
        this DbContext context,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Executes <paramref name="action"/> inside a database transaction and returns a result.
    /// </summary>
    /// <typeparam name="T">The return type of <paramref name="action"/>.</typeparam>
    /// <example>
    /// <code>
    /// var orderId = await context.ExecuteInTransactionAsync(async () =>
    /// {
    ///     context.Orders.Add(order);
    ///     await context.SaveChangesAsync();
    ///     return order.Id;
    /// });
    /// </code>
    /// </example>
    public static async Task<T> ExecuteInTransactionAsync<T>(
        this DbContext context,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await tx.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Detaches all tracked entities from the change tracker.
    /// Useful after bulk reads in long-running services to release memory.
    /// </summary>
    public static void DetachAll(this DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;
    }

    /// <summary>
    /// Removes all rows from the table for <typeparamref name="T"/> using <c>TRUNCATE TABLE</c>.
    /// Much faster than <c>RemoveRange</c> on large tables.
    /// <para><b>Warning:</b> bypasses EF Core interceptors, soft-delete filters, and foreign-key constraints.</para>
    /// </summary>
    public static async Task TruncateAsync<T>(
        this DbContext context,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var entityType = context.Model.FindEntityType(typeof(T))
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(T).Name}' is not registered in the model.");

        var tableName = entityType.GetTableName()!;
        var schema    = entityType.GetSchema();
        var fullName  = schema is null
            ? $"\"{tableName}\""
            : $"\"{schema}\".\"{tableName}\"";

#pragma warning disable EF1002 // fullName is constructed from EF model metadata, not user input
        await context.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {fullName}", cancellationToken);
#pragma warning restore EF1002
    }
}
