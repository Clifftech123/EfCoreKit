namespace EfCore.Extensions.Abstractions.Interfaces;

/// <summary>
/// Marker interface for entities that are scoped to a specific tenant.
/// Entities implementing this interface will have automatic tenant filtering
/// applied in queries and tenant assignment on save.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// Gets or sets the tenant identifier that owns this entity.
    /// </summary>
    string? TenantId { get; set; }
}
