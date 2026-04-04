using System.Linq.Expressions;

namespace EfCoreKit.Interfaces;

/// <summary>
/// Defines a reusable query specification that encapsulates filtering, ordering,
/// paging, and include logic for querying entities of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entity type this specification applies to.</typeparam>
public interface ISpecification<T> where T : class
{
    /// <summary>Gets the filter expression to apply. <c>null</c> means no filtering.</summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Gets the list of strongly-typed navigation property includes.</summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>Gets the list of string-based navigation property includes (e.g. <c>"Order.Items"</c>).</summary>
    List<string> IncludeStrings { get; }

    /// <summary>Gets the primary ascending order expression. <c>null</c> means no ordering.</summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>Gets the primary descending order expression. <c>null</c> means no ordering.</summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets additional sort columns applied after <see cref="OrderBy"/> or <see cref="OrderByDescending"/>.
    /// Each entry is <c>(KeySelector, Ascending)</c>.
    /// </summary>
    List<(Expression<Func<T, object>> KeySelector, bool Ascending)> ThenByExpressions { get; }

    /// <summary>Gets the maximum number of results to return. <c>null</c> means no limit.</summary>
    int? Take { get; }

    /// <summary>Gets the number of results to skip. <c>null</c> means no offset.</summary>
    int? Skip { get; }

    /// <summary>Gets whether the query should run with no-tracking behaviour.</summary>
    bool AsNoTracking { get; }

    /// <summary>Gets whether the query should use split-query execution to avoid cartesian explosion.</summary>
    bool AsSplitQuery { get; }
}

/// <summary>
/// Specification that includes a projection selector — use when you want to return
/// a DTO directly instead of the full entity.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public interface ISpecification<T, TResult> : ISpecification<T> where T : class
{
    /// <summary>The projection expression mapping <typeparamref name="T"/> to <typeparamref name="TResult"/>.</summary>
    Expression<Func<T, TResult>>? Selector { get; }
}
