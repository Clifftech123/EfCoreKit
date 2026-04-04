namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Marker interface for entities that support soft delete.
/// When an entity implementing this interface is deleted, it is marked
/// as deleted rather than being physically removed from the database.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the entity was soft-deleted.
    /// <c>null</c> if the entity has not been deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted the entity.
    /// <c>null</c> if the entity has not been deleted or no user context was available.
    /// </summary>
    string? DeletedBy { get; set; }
}