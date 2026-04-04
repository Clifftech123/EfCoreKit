using System.Linq.Expressions;
using EfCore.Extensions.Abstractions.Exceptions;
using EfCore.Extensions.Abstractions.Interfaces;
using EfCore.Extensions.Abstractions.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Extensions.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="DbSet{T}"/> providing common query shortcuts.
/// </summary>
/// <remarks>
/// These are additive convenience methods — all standard <see cref="DbSet{T}"/> and
/// EF Core APIs (<c>Add</c>, <c>Attach</c>, <c>Entry</c>, <c>FromSqlRaw</c>,
/// <c>ExecuteDeleteAsync</c>, <c>ExecuteUpdateAsync</c>, etc.) remain fully available
/// and can be used alongside these helpers.
/// </remarks>
public static class DbSetExtensions
{
    // ─── Single-entity lookups ────────────────────────────────────────

    /// <summary>
    /// Returns the entity with the given primary key, or <c>null</c> if not found.
    /// Checks the change tracker before hitting the database.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to search.</param>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    /// <example>
    /// <code>
    /// Order? order = await context.Orders.GetByIdAsync(orderId);
    /// </code>
    /// </example>
    public static async Task<T?> GetByIdAsync<T>(
        this DbSet<T> dbSet,
        object id,
        CancellationToken cancellationToken = default) where T : class
    {
        return await dbSet.FindAsync([id], cancellationToken);
    }

    /// <summary>
    /// Returns the entity with the given primary key, or throws
    /// <see cref="EntityNotFoundException"/> if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to search.</param>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when no entity with the given key exists.</exception>
    /// <example>
    /// <code>
    /// Order order = await context.Orders.GetByIdOrThrowAsync(orderId);
    /// </code>
    /// </example>
    public static async Task<T> GetByIdOrThrowAsync<T>(
        this DbSet<T> dbSet,
        object id,
        CancellationToken cancellationToken = default) where T : class
    {
        var entity = await dbSet.FindAsync([id], cancellationToken);
        return entity ?? throw new EntityNotFoundException(typeof(T).Name, id);
    }

    /// <summary>
    /// Returns the first entity matching the predicate, or <c>null</c> if none match.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to search.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The first matching entity, or <c>null</c>.</returns>
    /// <example>
    /// <code>
    /// Customer? customer = await context.Customers
    ///     .FirstOrDefaultAsync(c => c.Email == "jane@example.com");
    /// </code>
    /// </example>
    public static async Task<T?> FirstOrDefaultAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Returns the single entity matching the predicate, or <c>null</c> if none match.
    /// Throws <see cref="InvalidOperationException"/> if more than one entity matches.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to search.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The single matching entity, or <c>null</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when more than one entity matches.</exception>
    /// <example>
    /// <code>
    /// Customer? customer = await context.Customers
    ///     .SingleOrDefaultAsync(c => c.TaxId == taxId);
    /// </code>
    /// </example>
    public static async Task<T?> SingleOrDefaultAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).SingleOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Returns the last entity matching the predicate, or <c>null</c> if none match.
    /// <para><b>Important:</b> EF Core requires the query to be ordered.
    /// Apply an <c>OrderBy</c> before calling this method.</para>
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to search.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The last matching entity, or <c>null</c>.</returns>
    /// <example>
    /// <code>
    /// Order? latest = await context.Orders
    ///     .LastOrDefaultAsync(o => o.CustomerId == customerId);
    /// </code>
    /// </example>
    public static async Task<T?> LastOrDefaultAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).LastOrDefaultAsync(predicate, cancellationToken);
    }

    // ─── Multi-entity lookups ─────────────────────────────────────────

    /// <summary>
    /// Returns all entities in the set as a read-only list.
    /// Avoid on large tables — prefer a filtered or paginated query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>All entities as a read-only list.</returns>
    /// <example>
    /// <code>
    /// IReadOnlyList&lt;Category&gt; categories = await context.Categories.GetAllAsync();
    /// </code>
    /// </example>
    public static async Task<IReadOnlyList<T>> GetAllAsync<T>(
        this DbSet<T> dbSet,
        CancellationToken cancellationToken = default) where T : class
    {
        return await dbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns all entities matching the predicate as a read-only list.
    /// Translates to a SQL <c>WHERE</c> clause.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to query.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>All matching entities as a read-only list.</returns>
    /// <example>
    /// <code>
    /// IReadOnlyList&lt;Product&gt; cheap = await context.Products
    ///     .FindAsync(p => p.Price &lt; 10m);
    /// </code>
    /// </example>
    public static async Task<IReadOnlyList<T>> FindAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).Where(predicate).ToListAsync(cancellationToken);
    }

    // ─── Existence and counting ───────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> if an entity with the given primary key exists.
    /// Checks the change tracker before hitting the database.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to search.</param>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the entity exists; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// bool exists = await context.Orders.ExistsAsync(orderId);
    /// </code>
    /// </example>
    public static async Task<bool> ExistsAsync<T>(
        this DbSet<T> dbSet,
        object id,
        CancellationToken cancellationToken = default) where T : class
    {
        var entity = await dbSet.FindAsync([id], cancellationToken);
        return entity is not null;
    }

    /// <summary>
    /// Returns <c>true</c> if any entity matches the predicate.
    /// Translates to SQL <c>EXISTS</c> — more efficient than counting for existence checks.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to search.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if any entity matches; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// bool hasVip = await context.Customers.ExistsAsync(c => c.IsVip);
    /// </code>
    /// </example>
    public static async Task<bool> ExistsAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Returns the number of entities, optionally filtered by <paramref name="predicate"/>.
    /// Use <see cref="LongCountAsync"/> for tables that may exceed <see cref="int.MaxValue"/> rows.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to count.</param>
    /// <param name="predicate">Optional filter. Pass <c>null</c> to count all rows.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of matching entities.</returns>
    /// <example>
    /// <code>
    /// int total  = await context.Products.CountAsync();
    /// int active = await context.Products.CountAsync(p => p.IsActive);
    /// </code>
    /// </example>
    public static async Task<int> CountAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = (IQueryable<T>)dbSet;
        return predicate is null
            ? await query.CountAsync(cancellationToken)
            : await query.CountAsync(predicate, cancellationToken);
    }

    // ─── Write operations ─────────────────────────────────────────────

    /// <summary>
    /// Loads all entities matching the predicate then stages them for deletion.
    /// Changes are not persisted until <c>SaveChangesAsync</c> is called on the context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to remove from.</param>
    /// <param name="predicate">The filter identifying entities to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <example>
    /// <code>
    /// await context.Orders.RemoveRangeAsync(o => o.Status == OrderStatus.Cancelled);
    /// await context.SaveChangesAsync();
    /// </code>
    /// </example>
    public static async Task RemoveRangeAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        var entities = await ((IQueryable<T>)dbSet).Where(predicate).ToListAsync(cancellationToken);
        dbSet.RemoveRange(entities);
    }

    // ─── Aggregates ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the maximum value of the selected property across all entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The type of the selected property.</typeparam>
    /// <param name="dbSet">The DbSet to aggregate.</param>
    /// <param name="selector">Expression selecting the property to maximise.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The maximum value.</returns>
    /// <example>
    /// <code>
    /// decimal highest = await context.Orders.MaxAsync(o => o.Total);
    /// </code>
    /// </example>
    public static async Task<TResult> MaxAsync<T, TResult>(
        this DbSet<T> dbSet,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).MaxAsync(selector, cancellationToken);
    }

    /// <summary>
    /// Returns the minimum value of the selected property across all entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The type of the selected property.</typeparam>
    /// <param name="dbSet">The DbSet to aggregate.</param>
    /// <param name="selector">Expression selecting the property to minimise.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The minimum value.</returns>
    /// <example>
    /// <code>
    /// decimal lowest = await context.Orders.MinAsync(o => o.Total);
    /// </code>
    /// </example>
    public static async Task<TResult> MinAsync<T, TResult>(
        this DbSet<T> dbSet,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).MinAsync(selector, cancellationToken);
    }

    /// <summary>
    /// Returns the sum of the selected <c>decimal</c> property across all entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to aggregate.</param>
    /// <param name="selector">Expression selecting the <c>decimal</c> property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The sum.</returns>
    /// <example>
    /// <code>
    /// decimal revenue = await context.Orders.SumAsync(o => o.Total);
    /// </code>
    /// </example>
    public static async Task<decimal> SumAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).SumAsync(selector, cancellationToken);
    }

    /// <summary>
    /// Returns the average of the selected <c>int</c> property across all entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to aggregate.</param>
    /// <param name="selector">Expression selecting the <c>int</c> property to average.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The average as <c>double</c>.</returns>
    /// <example>
    /// <code>
    /// double avgQty = await context.OrderLines.AverageAsync(l => l.Quantity);
    /// </code>
    /// </example>
    public static async Task<double> AverageAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).AverageAsync(selector, cancellationToken);
    }

    /// <summary>
    /// Returns the average of the selected <c>decimal</c> property across all entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to aggregate.</param>
    /// <param name="selector">Expression selecting the <c>decimal</c> property to average.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The average as <c>decimal</c>.</returns>
    /// <example>
    /// <code>
    /// decimal avgPrice = await context.Products.AverageAsync(p => p.Price);
    /// </code>
    /// </example>
    public static async Task<decimal> AverageAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).AverageAsync(selector, cancellationToken);
    }

    /// <summary>
    /// Returns the average of the selected <c>double</c> property across all entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to aggregate.</param>
    /// <param name="selector">Expression selecting the <c>double</c> property to average.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The average as <c>double</c>.</returns>
    public static async Task<double> AverageAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).AverageAsync(selector, cancellationToken);
    }

    /// <summary>
    /// Returns the sum of the selected <c>int</c> property across all entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to aggregate.</param>
    /// <param name="selector">Expression selecting the <c>int</c> property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The sum as <c>int</c>.</returns>
    public static async Task<int> SumAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, int>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).SumAsync(selector, cancellationToken);
    }

    /// <summary>
    /// Returns the sum of the selected <c>long</c> property across all entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to aggregate.</param>
    /// <param name="selector">Expression selecting the <c>long</c> property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The sum as <c>long</c>.</returns>
    public static async Task<long> SumAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, long>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).SumAsync(selector, cancellationToken);
    }

    /// <summary>
    /// Returns the sum of the selected <c>double</c> property across all entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to aggregate.</param>
    /// <param name="selector">Expression selecting the <c>double</c> property to sum.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The sum as <c>double</c>.</returns>
    public static async Task<double> SumAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, double>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).SumAsync(selector, cancellationToken);
    }

    /// <summary>
    /// Returns <c>true</c> if the set contains at least one entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dbSet">The DbSet to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the set is non-empty; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// bool hasOrders = await context.Orders.AnyAsync();
    /// </code>
    /// </example>
    public static async Task<bool> AnyAsync<T>(
        this DbSet<T> dbSet,
        CancellationToken cancellationToken = default) where T : class
    {
        return await ((IQueryable<T>)dbSet).AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the total count as a <c>long</c>. Use instead of <see cref="CountAsync"/>
    /// for tables that may exceed <see cref="int.MaxValue"/> rows.
    /// </summary>
    /// <example>
    /// <code>
    /// long total = await context.AuditLogs.LongCountAsync();
    /// long errors = await context.AuditLogs.LongCountAsync(l => l.Level == "Error");
    /// </code>
    /// </example>
    public static async Task<long> LongCountAsync<T>(
        this DbSet<T> dbSet,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var query = (IQueryable<T>)dbSet;
        return predicate is null
            ? await query.LongCountAsync(cancellationToken)
            : await query.LongCountAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Returns all entities whose key is contained in <paramref name="ids"/>.
    /// Translates to a single SQL <c>WHERE key IN (...)</c> query — no N+1.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The key property type.</typeparam>
    /// <param name="dbSet">The DbSet to query.</param>
    /// <param name="keySelector">Expression selecting the key property (e.g. <c>x =&gt; x.Id</c>).</param>
    /// <param name="ids">The key values to match.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>All matching entities as a read-only list.</returns>
    /// <example>
    /// <code>
    /// var orderIds = new[] { 1, 2, 3 };
    /// IReadOnlyList&lt;Order&gt; orders = await context.Orders
    ///     .GetByIdsAsync(o => o.Id, orderIds);
    /// </code>
    /// </example>
    public static async Task<IReadOnlyList<T>> GetByIdsAsync<T, TKey>(
        this DbSet<T> dbSet,
        Expression<Func<T, TKey>> keySelector,
        IEnumerable<TKey> ids,
        CancellationToken cancellationToken = default) where T : class
    {
        var idList = ids.ToList();
        var parameter = keySelector.Parameters[0];
        var containsMethod = typeof(List<TKey>).GetMethod("Contains", new[] { typeof(TKey) })!;
        var body = Expression.Call(Expression.Constant(idList), containsMethod, keySelector.Body);
        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        return await ((IQueryable<T>)dbSet).Where(lambda).ToListAsync(cancellationToken);
    }

    // ─── Soft-delete helpers ──────────────────────────────────────────

    /// <summary>
    /// Returns all soft-deleted entities, bypassing the global soft-delete filter.
    /// </summary>
    /// <example>
    /// <code>
    /// var deletedOrders = await context.Orders.GetDeletedAsync();
    /// </code>
    /// </example>
    public static async Task<IReadOnlyList<T>> GetDeletedAsync<T>(
        this DbSet<T> dbSet,
        CancellationToken cancellationToken = default)
        where T : class, ISoftDeletable
    {
        return await dbSet
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Restores a soft-deleted entity by clearing <c>IsDeleted</c>, <c>DeletedAt</c>,
    /// and <c>DeletedBy</c>. Call <c>SaveChangesAsync</c> after this.
    /// </summary>
    /// <example>
    /// <code>
    /// var order = await context.Orders.GetDeletedAsync().FirstOrDefault(...);
    /// context.Orders.Restore(order);
    /// await context.SaveChangesAsync();
    /// </code>
    /// </example>
    public static void Restore<T>(this DbSet<T> dbSet, T entity)
        where T : class, ISoftDeletable
    {
        entity.IsDeleted  = false;
        entity.DeletedAt  = null;
        entity.DeletedBy  = null;
    }

    /// <summary>
    /// Permanently deletes an entity from the database, even when soft-delete is enabled.
    /// Use for GDPR erasure or other hard-delete requirements.
    /// Call <c>SaveChangesAsync</c> after this.
    /// </summary>
    /// <example>
    /// <code>
    /// context.Orders.HardDelete(order);
    /// await context.SaveChangesAsync();
    /// </code>
    /// </example>
    public static void HardDelete<T>(this DbSet<T> dbSet, T entity)
        where T : class, ISoftDeletable
        => dbSet.Remove(entity);

}