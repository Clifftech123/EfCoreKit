namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Marker interface for entities that track creation and modification timestamps.
/// Properties are automatically set by the audit interceptor during <c>SaveChangesAsync</c>.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the entity was last updated.
    /// <c>null</c> if the entity has never been modified after creation.
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity.
    /// </summary>
    string? UpdatedBy { get; set; }
}

/// <summary>
/// Extended audit interface that includes full change history.
/// Entities implementing this interface will have all changes logged
/// to a dedicated audit log table.
/// </summary>
public interface IFullAuditable : IAuditable
{
}