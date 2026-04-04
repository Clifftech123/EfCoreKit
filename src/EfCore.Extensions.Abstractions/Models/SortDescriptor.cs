namespace EfCore.Extensions.Abstractions.Models;

/// <summary>
/// Describes a sort operation to apply to a query.
/// Used with dynamic query builders to apply sorting from API requests.
/// </summary>
public sealed class SortDescriptor
{
    /// <summary>
    /// Gets the property name to sort by.
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// Gets a value indicating the sort direction.
    /// <c>true</c> for ascending; <c>false</c> for descending. Default is <c>true</c>.
    /// </summary>
    public bool Ascending { get; init; } = true;
}
