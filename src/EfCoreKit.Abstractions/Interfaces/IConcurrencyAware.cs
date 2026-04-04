namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Marker interface for entities that support optimistic concurrency control
/// using a row version token.
/// </summary>
public interface IConcurrencyAware
{
    /// <summary>
    /// Gets or sets the row version used for optimistic concurrency checks.
    /// This value is automatically managed by the database provider.
    /// </summary>
    byte[] RowVersion { get; set; }
}
