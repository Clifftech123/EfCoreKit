using System.Linq.Expressions;
using EfCoreKit.Abstractions.Exceptions;
using EfCoreKit.Abstractions.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IQueryable{T}"/> providing pagination,
/// conditional filtering, and dynamic ordering.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Paginates the query results using offset-based pagination.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="PagedResult{T}"/> containing the paginated data.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="page"/> or <paramref name="pageSize"/> is less than 1,
    /// or <paramref name="pageSize"/> exceeds 1000.
    /// </exception>
    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) where T : class
    {
    
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 1000);

       
         var  totalCount = await query.CountAsync(cancellationToken);
         var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, page, pageSize);
    }

    /// <summary>
    /// Conditionally applies a filter to the query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="condition">Whether to apply the filter.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The filtered or original query.</returns>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
    {
        return condition ? query.Where(predicate) : query;
    }

    /// <summary>
    /// Conditionally applies a filter when the value is not null.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="value">The value to check for null.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The filtered or original query.</returns>
    public static IQueryable<T> WhereIfNotNull<T, TValue>(
        this IQueryable<T> query,
        TValue? value,
        Expression<Func<T, bool>> predicate) where T : class
    {
        return value is not null ? query.Where(predicate) : query;
    }

    /// <summary>
    /// Conditionally applies a filter when the string value is not null or whitespace.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="value">The string value to check.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The filtered or original query.</returns>
    public static IQueryable<T> WhereIfNotEmpty<T>(
        this IQueryable<T> query,
        string? value,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
    {
        return !string.IsNullOrWhiteSpace(value) ? query.Where(predicate) : query;
    }

    /// <summary>
    /// Dynamically orders the query by a property name.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="propertyName">The property name to order by (supports dot-separated paths).</param>
    /// <param name="ascending"><c>true</c> for ascending; <c>false</c> for descending.</param>
    /// <returns>The ordered query.</returns>
    public static IQueryable<T> OrderByDynamic<T>(
        this IQueryable<T> query,
        string propertyName,
        bool ascending = true) where T : class
    {
        var parameter =Expression.Parameter(typeof(T), "x");
        Expression propertyAccess = parameter;
        foreach (var prop in propertyName.Split('.'))
        {
            propertyAccess = Expression.PropertyOrField(propertyAccess, prop);
        }
        var orderByExp = Expression.Lambda(propertyAccess, parameter);

        var methodName = ascending ? "OrderBy" : "OrderByDescending";
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), propertyAccess.Type);

        return (IQueryable<T>)method.Invoke(null, [query, orderByExp])!;
    }

    /// <summary>
    /// Applies a collection of <see cref="FilterDescriptor"/> to the query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="filters">The filters to apply.</param>
    /// <returns>The filtered query.</returns>
    public static IQueryable<T> ApplyFilters<T>(
        this IQueryable<T> query,
        IEnumerable<FilterDescriptor>? filters) where T : class
    {
        // TODO: Iterate filters, build expressions per operator, apply Where
        var filterList = filters?.ToList();
         if (filterList is null || !filterList.Any()) return query;
            if (filterList.Any(f => string.IsNullOrWhiteSpace(f.Property)))
                throw new InvalidFilterException("Filter property name cannot be null or empty.");
                return query.Where(f => f.)

        if (filters is null) return query;
        throw new NotImplementedException();
    }

    /// <summary>
    /// Applies a collection of <see cref="SortDescriptor"/> to the query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="sorts">The sort descriptors to apply.</param>
    /// <returns>The sorted query.</returns>
    public static IQueryable<T> ApplySorts<T>(
        this IQueryable<T> query,
        IEnumerable<SortDescriptor>? sorts) where T : class
    {
        // TODO: Iterate sorts, apply OrderByDynamic / ThenByDynamic
        if (sorts is null) return query;
        throw new NotImplementedException();
    }

    // ─── Projection ──────────────────────────────────────────────────

    /// <summary>
    /// Projects each element of the query into a new form using the specified selector
    /// and materializes the results as a list.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="selector">A projection expression mapping <typeparamref name="T"/> to <typeparamref name="TResult"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of projected results.</returns>
    /// <example>
    /// <code>
    /// var dtos = await context.Customers
    ///     .Where(c => c.IsActive)
    ///     .SelectToListAsync(c => new CustomerDto { Name = c.Name, Email = c.Email });
    /// </code>
    /// </example>
    public static async Task<List<TResult>> SelectToListAsync<T, TResult>(
        this IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Projects each element of the query into a new form using the specified selector
    /// and returns the first result or <c>null</c>.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="selector">A projection expression mapping <typeparamref name="T"/> to <typeparamref name="TResult"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The projected result or <c>null</c>.</returns>
    public static async Task<TResult?> SelectFirstOrDefaultAsync<T, TResult>(
        this IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await query.Select(selector).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Projects each element of the query into a new form using the specified selector
    /// and returns an offset-based paginated result.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="selector">A projection expression mapping <typeparamref name="T"/> to <typeparamref name="TResult"/>.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="PagedResult{TResult}"/> containing the projected paginated data.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="page"/> or <paramref name="pageSize"/> is less than 1,
    /// or <paramref name="pageSize"/> exceeds 1000.
    /// </exception>
    /// <example>
    /// <code>
    /// var page = await context.Customers
    ///     .Where(c => c.IsActive)
    ///     .SelectToPagedAsync(
    ///         c => new CustomerDto { Name = c.Name },
    ///         page: 1,
    ///         pageSize: 20);
    /// </code>
    /// </example>
    public static async Task<PagedResult<TResult>> SelectToPagedAsync<T, TResult>(
        this IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, TResult>> selector,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) where T : class
    {
        // TODO: Validate page >= 1, pageSize >= 1 && <= 1000
        // TODO: Count total from source query, project + skip/take, return PagedResult
        throw new NotImplementedException();
    }

    /// <summary>
    /// Projects the query using the specified selector and returns distinct results.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="selector">A projection expression mapping <typeparamref name="T"/> to <typeparamref name="TResult"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of distinct projected results.</returns>
    public static async Task<List<TResult>> SelectDistinctAsync<T, TResult>(
        this IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await query.Select(selector).Distinct().ToListAsync(cancellationToken);
    }

    // ─── Sorting ─────────────────────────────────────────────────────

    /// <summary>
    /// Applies a secondary dynamic sort to an already-ordered query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The already-ordered source query.</param>
    /// <param name="propertyName">The property name to sort by (supports dot-separated paths).</param>
    /// <param name="ascending"><c>true</c> for ascending; <c>false</c> for descending.</param>
    /// <returns>The query with secondary ordering applied.</returns>
    public static IQueryable<T> ThenByDynamic<T>(
        this IQueryable<T> query,
        string propertyName,
        bool ascending = true) where T : class
    {
        // TODO: Same reflection pattern as OrderByDynamic but calls ThenBy / ThenByDescending
        throw new NotImplementedException();
    }

    // ─── Keyset Pagination ───────────────────────────────────────────

    /// <summary>
    /// Paginates the query using keyset (cursor-based) pagination.
    /// More efficient than offset pagination for large datasets.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The cursor key type.</typeparam>
    /// <param name="query">The source query (must be ordered).</param>
    /// <param name="keySelector">Expression selecting the cursor property.</param>
    /// <param name="cursor">The cursor value from the previous page, or <c>null</c> for the first page.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="KeysetPagedResult{T}"/> containing the page and next/previous cursors.</returns>
    public static async Task<KeysetPagedResult<T>> ToKeysetPagedAsync<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        TKey? cursor,
        int pageSize,
        CancellationToken cancellationToken = default) where T : class where TKey : IComparable<TKey>
    {
        // TODO: If cursor != null, apply Where(x => key > cursor)
        // TODO: Take(pageSize + 1) to detect hasMore
        // TODO: If items.Count > pageSize, pop last item, set hasMore = true
        // TODO: Encode nextCursor from last item's key, previousCursor from first item's key
        // TODO: Return KeysetPagedResult
        throw new NotImplementedException();
    }

    // ─── Specification ───────────────────────────────────────────────

    /// <summary>
    /// Applies an <see cref="ISpecification{T}"/> to the query, including
    /// criteria, includes, ordering, paging, no-tracking, and split-query settings.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <returns>The query with all specification rules applied.</returns>
    public static IQueryable<T> ApplySpecification<T>(
        this IQueryable<T> query,
        Abstractions.Interfaces.ISpecification<T> specification) where T : class
    {
        // TODO: Apply Criteria (Where)
        // TODO: Apply Includes (typed + string)
        // TODO: Apply OrderBy / OrderByDescending
        // TODO: Apply Skip / Take
        // TODO: Apply AsNoTracking
        // TODO: Apply AsSplitQuery
        throw new NotImplementedException();
    }

    // ─── Tracking ────────────────────────────────────────────────────

    /// <summary>
    /// Applies <c>AsNoTracking</c> to the query for read-only scenarios.
    /// Improves performance when entities will not be modified.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <returns>The query with no-tracking enabled.</returns>
    public static IQueryable<T> WithNoTracking<T>(
        this IQueryable<T> query) where T : class
    {
        // TODO: return query.AsNoTracking()
        throw new NotImplementedException();
    }
}
