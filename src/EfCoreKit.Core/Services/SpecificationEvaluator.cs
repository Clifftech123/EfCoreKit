using EfCoreKit.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Services;

/// <summary>
/// Evaluates an <see cref="ISpecification{T}"/> against an <see cref="IQueryable{T}"/>
/// to produce the final filtered, ordered, and paged query.
/// </summary>
internal sealed class SpecificationEvaluator
{
    /// <summary>
    /// Applies the specification to the given query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <returns>The query with the specification applied.</returns>
    public static IQueryable<T> GetQuery<T>(
        IQueryable<T> query,
        ISpecification<T> specification) where T : class
    {
        // TODO: Apply Criteria
        // TODO: Apply Includes and IncludeStrings
        // TODO: Apply OrderBy / OrderByDescending
        // TODO: Apply Skip / Take
        // TODO: Apply AsNoTracking / AsSplitQuery
        return query;
    }
}
