namespace EfCore.Extensions.Abstractions.Models;

/// <summary>
/// Describes a filter to apply to a query.
/// Used with dynamic query builders to apply filters from API requests.
/// </summary>
public sealed class FilterDescriptor
{
    /// <summary>
    /// Gets the property name to filter on.
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// Gets the comparison operator.
    /// Supported values: <c>eq</c>, <c>ne</c>, <c>gt</c>, <c>gte</c>,
    /// <c>lt</c>, <c>lte</c>, <c>contains</c>, <c>startswith</c>, <c>endswith</c>.
    /// </summary>
    public required string Operator { get; init; }

    /// <summary>
    /// Gets the value to compare against.
    /// </summary>
    public object? Value { get; init; }
}
