using System.Linq.Expressions;
using EfCoreKit.Abstractions.Interfaces;

namespace EfCoreKit.Core.Specifications;

/// <summary>
/// Combinators for composing two <see cref="ISpecification{T}"/> instances.
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Returns a new specification whose criteria is the logical AND of
    /// <paramref name="left"/> and <paramref name="right"/>.
    /// Includes from both specs are merged.
    /// </summary>
    /// <example>
    /// <code>
    /// var spec = new ActiveCustomerSpec().And(new VipCustomerSpec());
    /// var customers = await context.Customers.ApplySpecification(spec).ToListAsync();
    /// </code>
    /// </example>
    public static ISpecification<T> And<T>(
        this ISpecification<T> left,
        ISpecification<T> right)
        where T : class
        => new CombinedSpecification<T>(left, right, CombineOperator.And);

    /// <summary>
    /// Returns a new specification whose criteria is the logical OR of
    /// <paramref name="left"/> and <paramref name="right"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var spec = new PendingOrderSpec().Or(new OverdueOrderSpec());
    /// </code>
    /// </example>
    public static ISpecification<T> Or<T>(
        this ISpecification<T> left,
        ISpecification<T> right)
        where T : class
        => new CombinedSpecification<T>(left, right, CombineOperator.Or);
}

internal enum CombineOperator { And, Or }

internal sealed class CombinedSpecification<T> : Specification<T> where T : class
{
    public CombinedSpecification(ISpecification<T> left, ISpecification<T> right, CombineOperator op)
    {
        if (left.Criteria is not null && right.Criteria is not null)
        {
            var param     = left.Criteria.Parameters[0];
            var rightBody = new ParameterReplacer(right.Criteria.Parameters[0], param)
                .Visit(right.Criteria.Body);

            var combined = op == CombineOperator.And
                ? Expression.AndAlso(left.Criteria.Body, rightBody)
                : Expression.OrElse(left.Criteria.Body, rightBody);

            AddCriteria(Expression.Lambda<Func<T, bool>>(combined, param));
        }
        else
        {
            if (left.Criteria  is not null) AddCriteria(left.Criteria);
            if (right.Criteria is not null) AddCriteria(right.Criteria);
        }

        foreach (var inc in left.Includes)        AddInclude(inc);
        foreach (var inc in right.Includes)       AddInclude(inc);
        foreach (var inc in left.IncludeStrings)  AddInclude(inc);
        foreach (var inc in right.IncludeStrings) AddInclude(inc);
    }
}

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _from;
    private readonly ParameterExpression _to;

    public ParameterReplacer(ParameterExpression from, ParameterExpression to)
        => (_from, _to) = (from, to);

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _from ? _to : base.VisitParameter(node);
}
