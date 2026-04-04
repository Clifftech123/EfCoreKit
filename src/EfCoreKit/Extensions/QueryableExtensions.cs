using System.Linq.Expressions;
using EfCoreKit.Exceptions;
using EfCoreKit.Interfaces;
using EfCoreKit.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Extensions;

/// <summary>
/// Extension methods for <see cref="IQueryable{T}"/> providing pagination,
/// conditional filtering, and dynamic ordering.
/// </summary>
/// <remarks>
/// These are additive convenience methods — all standard LINQ and EF Core
/// <see cref="IQueryable{T}"/> operators (<c>Where</c>, <c>Select</c>, <c>Include</c>,
/// <c>GroupBy</c>, <c>Join</c>, etc.) continue to work and can be mixed freely
/// with the helpers below.
/// </remarks>
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
    /// <example>
    /// <code>
    /// var page = await context.Orders
    ///     .Where(o => o.Status == OrderStatus.Active)
    ///     .OrderBy(o => o.CreatedAt)
    ///     .ToPagedAsync(page: 2, pageSize: 25);
    ///
    /// // page.Items      → items on the current page
    /// // page.TotalCount  → total matching rows
    /// // page.TotalPages  → calculated page count
    /// </code>
    /// </example>
    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 1000);

        var totalCount = await query.CountAsync(cancellationToken);
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
    /// <example>
    /// <code>
    /// bool onlyActive = true;
    /// var results = await context.Products
    ///     .WhereIf(onlyActive, p => p.IsActive)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate) where T : class
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
    /// <example>
    /// <code>
    /// int? minPrice = request.MinPrice; // may be null
    /// var results = await context.Products
    ///     .WhereIfNotNull(minPrice, p => p.Price >= minPrice)
    ///     .ToListAsync();
    /// </code>
    /// </example>
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
    /// <example>
    /// <code>
    /// string? searchTerm = request.Search; // may be null or empty
    /// var results = await context.Products
    ///     .WhereIfNotEmpty(searchTerm, p => p.Name.Contains(searchTerm!))
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<T> WhereIfNotEmpty<T>(
        this IQueryable<T> query,
        string? value,
        Expression<Func<T, bool>> predicate) where T : class
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
    /// <example>
    /// <code>
    /// // Sort by a column name received from an API request
    /// var sorted = context.Products
    ///     .OrderByDynamic("Price", ascending: false);
    ///
    /// // Nested property path
    /// var sorted = context.Customers
    ///     .OrderByDynamic("Address.City");
    /// </code>
    /// </example>
    public static IQueryable<T> OrderByDynamic<T>(
        this IQueryable<T> query,
        string propertyName,
        bool ascending = true) where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression propertyAccess = parameter;
        foreach (var prop in propertyName.Split('.'))
        {
            propertyAccess = Expression.PropertyOrField(propertyAccess, prop);
        }
        var orderByExp = System.Linq.Expressions.Expression.Lambda(propertyAccess, parameter);

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
    /// <example>
    /// <code>
    /// var filters = new[]
    /// {
    ///     new FilterDescriptor { Field = "Status",  Operator = "eq",       Value = "Active" },
    ///     new FilterDescriptor { Field = "Price",   Operator = "gte",      Value = 9.99m },
    ///     new FilterDescriptor { Field = "Name",    Operator = "contains", Value = "Pro" }
    /// };
    ///
    /// var results = await context.Products
    ///     .ApplyFilters(filters)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<T> ApplyFilters<T>(
        this IQueryable<T> query,
        IEnumerable<FilterDescriptor>? filters) where T : class
    {
        var filterList = filters?.ToList();
        if (filterList is null || filterList.Count == 0) return query;

        if (filterList.Any(f => string.IsNullOrWhiteSpace(f.Field)))
            throw new InvalidFilterException("Filter field name cannot be null or empty.");

        var parameter = Expression.Parameter(typeof(T), "x");

        foreach (var filter in filterList)
        {
            Expression propertyAccess = parameter;
            foreach (var segment in filter.Field.Split('.'))
                propertyAccess = Expression.PropertyOrField(propertyAccess, segment);

            var propertyType = propertyAccess.Type;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var convertedValue = filter.Value is null
                ? null
                : Convert.ChangeType(filter.Value, underlyingType);
            var constant = Expression.Constant(convertedValue, propertyType);

            Expression body = filter.Operator.ToLowerInvariant() switch
            {
                "eq"         => Expression.Equal(propertyAccess, constant),
                "ne"         => Expression.NotEqual(propertyAccess, constant),
                "gt"         => Expression.GreaterThan(propertyAccess, constant),
                "gte"        => Expression.GreaterThanOrEqual(propertyAccess, constant),
                "lt"         => Expression.LessThan(propertyAccess, constant),
                "lte"        => Expression.LessThanOrEqual(propertyAccess, constant),
                "contains"   => Expression.Call(propertyAccess,
                                    typeof(string).GetMethod("Contains", [typeof(string)])!,
                                    constant),
                "startswith" => Expression.Call(propertyAccess,
                                    typeof(string).GetMethod("StartsWith", [typeof(string)])!,
                                    constant),
                "endswith"   => Expression.Call(propertyAccess,
                                    typeof(string).GetMethod("EndsWith", [typeof(string)])!,
                                    constant),
                "isnull"     => Expression.Equal(propertyAccess,
                                    Expression.Constant(null, propertyType)),
                "isnotnull"  => Expression.NotEqual(propertyAccess,
                                    Expression.Constant(null, propertyType)),
                "in"         => BuildInExpression(propertyAccess, filter.Value, underlyingType),
                "between"    => BuildBetweenExpression(propertyAccess, filter.Value, propertyType, underlyingType),
                _ => throw new InvalidFilterException($"Unsupported filter operator: '{filter.Operator}'.")
            };

            query = query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
        }

        return query;
    }

    /// <summary>
    /// Applies a collection of <see cref="SortDescriptor"/> to the query.
    /// Supports multi-column sorting — first descriptor maps to <c>OrderBy</c>,
    /// subsequent descriptors map to <c>ThenBy</c>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="sorts">The sort descriptors to apply.</param>
    /// <returns>The sorted query.</returns>
    /// <example>
    /// <code>
    /// var sorts = new[]
    /// {
    ///     new SortDescriptor { Field = "Category", Ascending = true },
    ///     new SortDescriptor { Field = "Price",    Ascending = false }
    /// };
    ///
    /// var sorted = context.Products.ApplySorts(sorts);
    /// </code>
    /// </example>
    public static IQueryable<T> ApplySorts<T>(
        this IQueryable<T> query,
        IEnumerable<SortDescriptor>? sorts) where T : class
    {
        var sortList = sorts?.ToList();
        if (sortList is null || sortList.Count == 0) return query;

        query = query.OrderByDynamic(sortList[0].Field, sortList[0].Ascending);

        for (var i = 1; i < sortList.Count; i++)
            query = query.ThenByDynamic(sortList[i].Field, sortList[i].Ascending);

        return query;
    }

    /// <summary>
    /// Applies a secondary dynamic sort to an already-ordered query.
    /// Supports dot-separated nested property paths (e.g. <c>"Address.City"</c>).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The already-ordered source query.</param>
    /// <param name="propertyName">The property name to sort by.</param>
    /// <param name="ascending"><c>true</c> for ascending; <c>false</c> for descending.</param>
    /// <returns>The query with secondary ordering applied.</returns>
    /// <example>
    /// <code>
    /// var results = context.Products
    ///     .OrderByDynamic("Category")
    ///     .ThenByDynamic("Price", ascending: false);
    /// </code>
    /// </example>
    public static IQueryable<T> ThenByDynamic<T>(
        this IQueryable<T> query,
        string propertyName,
        bool ascending = true) where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression propertyAccess = parameter;
        foreach (var prop in propertyName.Split('.'))
            propertyAccess = Expression.PropertyOrField(propertyAccess, prop);

        var orderByExp = Expression.Lambda(propertyAccess, parameter);
        var methodName = ascending ? "ThenBy" : "ThenByDescending";
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), propertyAccess.Type);

        return (IQueryable<T>)method.Invoke(null, [query, orderByExp])!;
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
    /// <example>
    /// <code>
    /// var email = await context.Customers
    ///     .Where(c => c.Id == customerId)
    ///     .SelectFirstOrDefaultAsync(c => c.Email);
    /// </code>
    /// </example>
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
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 1000);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>(items, totalCount, page, pageSize);
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
    /// <example>
    /// <code>
    /// List&lt;string&gt; cities = await context.Customers
    ///     .SelectDistinctAsync(c => c.Address.City);
    /// </code>
    /// </example>
    public static async Task<List<TResult>> SelectDistinctAsync<T, TResult>(
        this IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default) where T : class
    {
        return await query.Select(selector).Distinct().ToListAsync(cancellationToken);
    }

    // ─── Keyset pagination ────────────────────────────────────────────

    /// <summary>
    /// Paginates the query using keyset (cursor-based) pagination.
    /// More efficient than offset pagination on large datasets because it avoids
    /// scanning skipped rows — ideal for infinite scroll and high-volume feeds.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The cursor key type — must be orderable.</typeparam>
    /// <param name="query">The source query. Must be ordered by the same key as <paramref name="keySelector"/>.</param>
    /// <param name="keySelector">Expression selecting the cursor property (e.g. <c>x =&gt; x.Id</c>).</param>
    /// <param name="cursor">The cursor value from the previous page, or <c>null</c> for the first page.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="KeysetPagedResult{T}"/> with items, next/previous cursors, and a has-more flag.</returns>
    /// <example>
    /// <code>
    /// // First page
    /// var first = await context.Orders
    ///     .OrderBy(o => o.Id)
    ///     .ToKeysetPagedAsync(o => o.Id, cursor: null, pageSize: 50);
    ///
    /// // Next page — pass the cursor from the previous result
    /// var next = await context.Orders
    ///     .OrderBy(o => o.Id)
    ///     .ToKeysetPagedAsync(o => o.Id, cursor: int.Parse(first.NextCursor!), pageSize: 50);
    /// </code>
    /// </example>
    public static async Task<KeysetPagedResult<T>> ToKeysetPagedAsync<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        TKey? cursor,
        int pageSize,
        CancellationToken cancellationToken = default)
        where T : class
        where TKey : IComparable<TKey>
    {
        if (cursor is not null)
        {
            var parameter = keySelector.Parameters[0];
            var greaterThan = Expression.GreaterThan(
                keySelector.Body,
                Expression.Constant(cursor, typeof(TKey)));
            query = query.Where(Expression.Lambda<Func<T, bool>>(greaterThan, parameter));
        }

        var items = await query.Take(pageSize + 1).ToListAsync(cancellationToken);
        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        var compiledKey = keySelector.Compile();
        var nextCursor = hasMore ? compiledKey(items[^1])?.ToString() : null;
        var previousCursor = cursor?.ToString();

        return new KeysetPagedResult<T>(items, nextCursor, previousCursor, hasMore);
    }

    // ─── Specification ────────────────────────────────────────────────

    /// <summary>
    /// Applies all rules from an <see cref="Abstractions.Interfaces.ISpecification{T}"/> to the query —
    /// criteria, includes, ordering, paging, no-tracking, and split-query settings.
    /// Lets callers express the full shape of a query as a single reusable object.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <returns>The query with all specification rules applied.</returns>
    /// <example>
    /// <code>
    /// var spec = new ActiveCustomersSpec(minAge: 18);
    /// var customers = await context.Customers
    ///     .ApplySpecification(spec)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<T> ApplySpecification<T>(
        this IQueryable<T> query,
        ISpecification<T> specification) where T : class
    {
        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        query = specification.Includes
            .Aggregate(query, (q, include) => q.Include(include));

        query = specification.IncludeStrings
            .Aggregate(query, (q, include) => q.Include(include));

        if (specification.OrderBy is not null)
            query = query.OrderBy(specification.OrderBy);
        else if (specification.OrderByDescending is not null)
            query = query.OrderByDescending(specification.OrderByDescending);

        if (specification.ThenByExpressions.Count > 0)
        {
            var ordered = query as IOrderedQueryable<T>
                ?? throw new InvalidOperationException(
                    "ThenBy requires OrderBy or OrderByDescending to be set on the specification first.");
            foreach (var (keySelector, ascending) in specification.ThenByExpressions)
                ordered = ascending ? ordered.ThenBy(keySelector) : ordered.ThenByDescending(keySelector);
            query = ordered;
        }

        if (specification.Skip.HasValue)
            query = query.Skip(specification.Skip.Value);

        if (specification.Take.HasValue)
            query = query.Take(specification.Take.Value);

        if (specification.AsNoTracking)
            query = query.AsNoTracking();

        if (specification.AsSplitQuery)
            query = query.AsSplitQuery();

        return query;
    }

    // ─── Tracking ─────────────────────────────────────────────────────

    /// <summary>
    /// Applies <c>AsNoTracking</c> to the query for read-only scenarios.
    /// EF Core will not track returned entities — improves performance when
    /// entities will not be modified and saved back to the database.
    /// </summary>
    public static IQueryable<T> WithNoTracking<T>(
        this IQueryable<T> query) where T : class
        => query.AsNoTracking();

    // ─── Soft-delete visibility ───────────────────────────────────────

    /// <summary>
    /// Bypasses the global soft-delete filter and returns all entities, including deleted ones.
    /// </summary>
    /// <example>
    /// <code>
    /// var all = await context.Orders.IncludeDeleted().ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<T> IncludeDeleted<T>(
        this IQueryable<T> query)
        where T : class, ISoftDeletable
        => query.IgnoreQueryFilters();

    /// <summary>
    /// Returns only soft-deleted entities (bypasses the global filter and adds <c>WHERE IsDeleted = true</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// var deleted = await context.Orders.OnlyDeleted().ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<T> OnlyDeleted<T>(
        this IQueryable<T> query)
        where T : class, ISoftDeletable
        => query.IgnoreQueryFilters().Where(e => e.IsDeleted);

    // ─── Specification + pagination shortcut ─────────────────────────

    /// <summary>
    /// Applies a specification and paginates the result in one call.
    /// </summary>
    /// <example>
    /// <code>
    /// var page = await context.Orders
    ///     .ToPagedFromSpecAsync(new RecentOrdersSpec(), page: 1, pageSize: 20);
    /// </code>
    /// </example>
    public static Task<PagedResult<T>> ToPagedFromSpecAsync<T>(
        this IQueryable<T> query,
        ISpecification<T> specification,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        where T : class
        => query.ApplySpecification(specification).ToPagedAsync(page, pageSize, cancellationToken);

    /// <summary>
    /// Applies a projecting specification and materialises the results as a list of
    /// <typeparamref name="TResult"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var dtos = await context.Orders.ToListAsync(new OrderSummarySpec());
    /// </code>
    /// </example>
    public static async Task<List<TResult>> ToListAsync<T, TResult>(
        this IQueryable<T> query,
        ISpecification<T, TResult> specification,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (specification.Selector is null)
            throw new InvalidOperationException(
                $"Specification '{specification.GetType().Name}' has no Selector defined.");

        return await query
            .ApplySpecification(specification)
            .Select(specification.Selector)
            .ToListAsync(cancellationToken);
    }

    // ─── Filter helpers (private) ─────────────────────────────────────

    private static Expression BuildInExpression(Expression propertyAccess, object? value, Type underlyingType)
    {
        if (value is not System.Collections.IEnumerable enumerable)
            throw new InvalidFilterException("'in' operator requires an IEnumerable value.");

        var listType      = typeof(List<>).MakeGenericType(underlyingType);
        var list          = Activator.CreateInstance(listType)!;
        var addMethod     = listType.GetMethod("Add")!;
        var containsMethod = listType.GetMethod("Contains", [underlyingType])!;

        foreach (var item in enumerable)
            addMethod.Invoke(list, [Convert.ChangeType(item, underlyingType)]);

        return Expression.Call(Expression.Constant(list), containsMethod, propertyAccess);
    }

    private static Expression BuildBetweenExpression(
        Expression propertyAccess, object? value, Type propertyType, Type underlyingType)
    {
        if (value is not object[] range || range.Length != 2)
            throw new InvalidFilterException("'between' operator requires a 2-element object[] value: [min, max].");

        var min = Expression.Constant(Convert.ChangeType(range[0], underlyingType), propertyType);
        var max = Expression.Constant(Convert.ChangeType(range[1], underlyingType), propertyType);

        return Expression.AndAlso(
            Expression.GreaterThanOrEqual(propertyAccess, min),
            Expression.LessThanOrEqual(propertyAccess, max));
    }
}
