namespace EfCoreKit.Core.Specifications;

/// <summary>
/// Fluent builder for constructing <see cref="Specification{T}"/> instances inline
/// without requiring a dedicated class.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public sealed class SpecificationBuilder<T> : Specification<T> where T : class
{
    // Expose the protected methods as public for the builder pattern.

    /// <inheritdoc cref="Specification{T}.AddCriteria"/>
    public new SpecificationBuilder<T> AddCriteria(System.Linq.Expressions.Expression<Func<T, bool>> criteria)
    {
        base.AddCriteria(criteria);
        return this;
    }

    /// <inheritdoc cref="Specification{T}.AddInclude(System.Linq.Expressions.Expression{Func{T, object}})"/>
    public new SpecificationBuilder<T> AddInclude(System.Linq.Expressions.Expression<Func<T, object>> includeExpression)
    {
        base.AddInclude(includeExpression);
        return this;
    }

    /// <inheritdoc cref="Specification{T}.AddInclude(string)"/>
    public new SpecificationBuilder<T> AddInclude(string includeString)
    {
        base.AddInclude(includeString);
        return this;
    }

    /// <inheritdoc cref="Specification{T}.ApplyOrderBy"/>
    public new SpecificationBuilder<T> ApplyOrderBy(System.Linq.Expressions.Expression<Func<T, object>> orderByExpression)
    {
        base.ApplyOrderBy(orderByExpression);
        return this;
    }

    /// <inheritdoc cref="Specification{T}.ApplyOrderByDescending"/>
    public new SpecificationBuilder<T> ApplyOrderByDescending(System.Linq.Expressions.Expression<Func<T, object>> orderByDescExpression)
    {
        base.ApplyOrderByDescending(orderByDescExpression);
        return this;
    }

    /// <inheritdoc cref="Specification{T}.ApplyPaging"/>
    public new SpecificationBuilder<T> ApplyPaging(int skip, int take)
    {
        base.ApplyPaging(skip, take);
        return this;
    }

    /// <inheritdoc cref="Specification{T}.ApplyAsNoTracking"/>
    public new SpecificationBuilder<T> ApplyAsNoTracking()
    {
        base.ApplyAsNoTracking();
        return this;
    }

    /// <inheritdoc cref="Specification{T}.ApplyAsSplitQuery"/>
    public new SpecificationBuilder<T> ApplyAsSplitQuery()
    {
        base.ApplyAsSplitQuery();
        return this;
    }
}
