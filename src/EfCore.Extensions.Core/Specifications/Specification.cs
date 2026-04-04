using System.Linq.Expressions;
using EfCore.Extensions.Abstractions.Interfaces;

namespace EfCore.Extensions.Core.Specifications;

/// <summary>
/// Base implementation of <see cref="ISpecification{T}"/>.
/// Derive from this class to create reusable query specifications.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class Specification<T> : ISpecification<T> where T : class
{
    /// <inheritdoc />
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    /// <inheritdoc />
    public List<Expression<Func<T, object>>> Includes { get; } = [];

    /// <inheritdoc />
    public List<string> IncludeStrings { get; } = [];

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc />
    public List<(Expression<Func<T, object>> KeySelector, bool Ascending)> ThenByExpressions { get; } = [];

    /// <inheritdoc />
    public int? Take { get; private set; }

    /// <inheritdoc />
    public int? Skip { get; private set; }

    /// <inheritdoc />
    public bool AsNoTracking { get; private set; }

    /// <inheritdoc />
    public bool AsSplitQuery { get; private set; }

    /// <summary>
    /// Sets the filter criteria for this specification.
    /// </summary>
    /// <param name="criteria">The filter expression.</param>
    protected void AddCriteria(Expression<Func<T, bool>> criteria) =>
        Criteria = criteria;

    /// <summary>
    /// Adds a navigation property include.
    /// </summary>
    /// <param name="includeExpression">The include expression.</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression) =>
        Includes.Add(includeExpression);

    /// <summary>
    /// Adds a string-based navigation property include.
    /// </summary>
    /// <param name="includeString">The include path (e.g., <c>"Order.Items"</c>).</param>
    protected void AddInclude(string includeString) =>
        IncludeStrings.Add(includeString);

    /// <summary>
    /// Sets ascending ordering.
    /// </summary>
    /// <param name="orderByExpression">The order-by expression.</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) =>
        OrderBy = orderByExpression;

    /// <summary>
    /// Sets descending ordering.
    /// </summary>
    /// <param name="orderByDescExpression">The order-by-descending expression.</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression) =>
        OrderByDescending = orderByDescExpression;

    /// <summary>
    /// Sets paging.
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Enables no-tracking query execution.
    /// </summary>
    protected void ApplyAsNoTracking() => AsNoTracking = true;

    /// <summary>
    /// Enables split query execution.
    /// </summary>
    protected void ApplyAsSplitQuery() => AsSplitQuery = true;

    /// <summary>
    /// Adds a secondary sort column in ascending order.
    /// Must be called after <see cref="ApplyOrderBy"/> or <see cref="ApplyOrderByDescending"/>.
    /// </summary>
    protected void ApplyThenBy(Expression<Func<T, object>> keySelector)
        => ThenByExpressions.Add((keySelector, true));

    /// <summary>
    /// Adds a secondary sort column in descending order.
    /// </summary>
    protected void ApplyThenByDescending(Expression<Func<T, object>> keySelector)
        => ThenByExpressions.Add((keySelector, false));
}

/// <summary>
/// Base specification that projects query results to <typeparamref name="TResult"/>.
/// Use when you want a spec to return a DTO directly without loading the full entity.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TResult">The projected type.</typeparam>
public abstract class Specification<T, TResult> : Specification<T>, ISpecification<T, TResult>
    where T : class
{
    /// <inheritdoc />
    public Expression<Func<T, TResult>>? Selector { get; private set; }

    /// <summary>Sets the projection selector for this specification.</summary>
    protected void ApplySelector(Expression<Func<T, TResult>> selector) => Selector = selector;
}
