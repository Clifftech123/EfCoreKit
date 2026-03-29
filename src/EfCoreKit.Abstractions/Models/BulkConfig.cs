
namespace EfCoreKit.Abstractions.Models;

/// <summary>
/// Configuration options for bulk operations.
/// </summary>
public sealed class BulkConfig
{
    /// <summary>
    /// Number of records per batch. Default: 1000.
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Timeout in seconds. Default: 30.
    /// </summary>
    public int Timeout { get; set; } = 30;

    /// <summary>
    /// Whether to preserve insert order. Default: true.
    /// </summary>
    public bool PreserveInsertOrder { get; set; } = true;

    /// <summary>
    /// Whether to set output identity after insert. Default: false.
    /// </summary>
    public bool SetOutputIdentity { get; set; }

    /// <summary>
    /// Properties to update by (for upsert). Default: primary key.
    /// </summary>
    public IList<string>? UpdateByProperties { get; set; }

    /// <summary>
    /// Properties to include in update.
    /// </summary>
    public IList<string>? PropertiesToInclude { get; set; }

    /// <summary>
    /// Properties to exclude from update.
    /// </summary>
    public IList<string>? PropertiesToExclude { get; set; }

    /// <summary>
    /// Progress callback. Reports (current, total).
    /// </summary>
    public Action<int, int>? OnProgress { get; set; }

    /// <summary>
    /// Whether to use transactions. Default: true.
    /// </summary>
    public bool UseTransaction { get; set; } = true;

    /// <summary>
    /// Whether to track entities after operation. Default: false.
    /// </summary>
    public bool TrackEntities { get; set; }
}