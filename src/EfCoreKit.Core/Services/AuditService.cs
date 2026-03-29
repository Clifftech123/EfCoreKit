using EfCoreKit.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Services;

/// <summary>
/// Service responsible for recording detailed audit log entries
/// for entities implementing <see cref="IFullAuditable"/>.
/// </summary>
internal sealed class AuditService
{
    /// <summary>
    /// Records change details for the given context before saving.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> with pending changes.</param>
    public void RecordChanges(DbContext context)
    {
        // TODO: Iterate ChangeTracker entries implementing IFullAuditable
        // - Capture old/new values for each changed property
        // - Build audit log entries
    }
}
