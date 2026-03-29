using System.Linq.Expressions;

namespace EfCoreKit.Core.Internal;

/// <summary>
/// Utility methods for building and manipulating LINQ expressions at runtime.
/// </summary>
internal static class ExpressionHelper
{
    /// <summary>
    /// Builds a property access expression for the given property name on type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="propertyName">The property name (supports dot-separated nested paths).</param>
    /// <returns>A lambda expression accessing the property.</returns>
    public static LambdaExpression BuildPropertyAccessor<T>(string propertyName)
    {
        // TODO: Build parameter -> member access expression chain for nested properties
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression property = parameter;
        foreach (var member in propertyName.Split('.'))
        {
            property = Expression.PropertyOrField(property, member);
        }
        return Expression.Lambda(property, parameter);
    }
}
