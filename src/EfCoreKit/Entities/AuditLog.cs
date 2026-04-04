namespace EfCoreKit.Entities;

/// <summary>
/// Represents a single field-level change record for entities implementing
/// <see cref="EfCoreKit.Interfaces.IFullAuditable"/>.
/// Add a <c>DbSet&lt;AuditLog&gt;</c> to your DbContext and enable
/// <c>.EnableAuditTrail(fullLog: true)</c> to activate change history.
/// </summary>
public class AuditLog
{
    /// <summary>Gets or sets the surrogate primary key.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the entity type name (e.g. <c>"Order"</c>).</summary>
    public string EntityType { get; set; } = "";

    /// <summary>Gets or sets the primary key of the changed entity, serialised as a string.</summary>
    public string EntityKey { get; set; } = "";

    /// <summary>Gets or sets the name of the property that changed.</summary>
    public string PropertyName { get; set; } = "";

    /// <summary>Gets or sets the value before the change. <c>null</c> for newly added entities.</summary>
    public string? OldValue { get; set; }

    /// <summary>Gets or sets the value after the change. <c>null</c> for deleted entities.</summary>
    public string? NewValue { get; set; }

    /// <summary>Gets or sets the EF Core state: <c>Added</c>, <c>Modified</c>, or <c>Deleted</c>.</summary>
    public string Action { get; set; } = "";

    /// <summary>Gets or sets the UTC timestamp of the change.</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>Gets or sets the identifier of the user who made the change.</summary>
    public string? ChangedBy { get; set; }
}
