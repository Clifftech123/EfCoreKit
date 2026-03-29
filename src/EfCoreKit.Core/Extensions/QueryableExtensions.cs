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
        // TODO: Validate page >= 1, pageSize >= 1 && <= 1000
        // TODO: Count total, skip/take, return PagedResult
        throw new NotImplementedException();
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
        System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
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
        // TODO: Build expression from propertyName, call Queryable.OrderBy/OrderByDescending via reflection
        throw new NotImplementedException();
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
}
