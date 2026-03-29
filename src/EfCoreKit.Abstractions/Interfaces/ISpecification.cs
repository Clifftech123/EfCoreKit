using System.Linq.Expressions;

namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Defines a reusable query specification that encapsulates filtering, ordering,
/// paging, and include logic for querying entities of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entity type this specification applies to.</typeparam>
public interface ISpecification<T> where T : class
{
    /// <summary>
    /// Gets the filter expression to apply. <c>null</c> means no filtering.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Gets the list of strongly-typed navigation property includes.
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the list of string-based navigation property includes
    /// for multi-level include paths (e.g., <c>"Order.Items"</c>).
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the expression used for ascending ordering. <c>null</c> means no ordering.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the expression used for descending ordering. <c>null</c> means no ordering.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the maximum number of results to return. <c>null</c> means no limit.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets the number of results to skip. <c>null</c> means no offset.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets a value indicating whether the query should be executed with no-tracking behavior.
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// Gets a value indicating whether the query should use split query execution
    /// to avoid cartesian explosion with multiple includes.
    /// </summary>
    bool AsSplitQuery { get; }
}
