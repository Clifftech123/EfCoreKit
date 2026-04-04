using EfCoreKit.Abstractions.Entities;
using EfCoreKit.Abstractions.Interfaces;
using EfCoreKit.Core.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreKit.Core.Interceptors;

/// <summary>
/// Handles all audit concerns in a single interceptor:
/// <list type="bullet">
///   <item>Sets <c>CreatedAt/CreatedBy</c> on <see cref="IAuditable"/> entities when added.</item>
///   <item>Sets <c>UpdatedAt/UpdatedBy</c> on <see cref="IAuditable"/> entities when modified,
///         and protects <c>CreatedAt/CreatedBy</c> from being overwritten.</item>
///   <item>Writes field-level <see cref="AuditLog"/> records for <see cref="IFullAuditable"/>
///         entities when <see cref="EfCoreOptions.FullAuditLogEnabled"/> is <c>true</c>.
///         Requires a <c>DbSet&lt;AuditLog&gt;</c> on your DbContext.</item>
/// </list>
/// </summary>
internal sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly EfCoreOptions _options;
    private readonly IUserProvider? _userProvider;

    public AuditInterceptor(EfCoreOptions options, IUserProvider? userProvider = null)
    {
        _options      = options;
        _userProvider = userProvider;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Process(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Process(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Process(DbContext? context)
    {
        if (context is null) return;

        var now    = DateTime.UtcNow;
        var userId = _userProvider?.GetCurrentUserId();

        ApplyAuditFields(context, now, userId);

        if (_options.FullAuditLogEnabled)
            WriteAuditLogs(context, now, userId);
    }

    // ── Basic audit fields ────────────────────────────────────────────

    private static void ApplyAuditFields(DbContext context, DateTime now, string? userId)
    {
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                    break;
            }
        }
    }

    // ── Full audit log (field-level change history) ───────────────────

    private static void WriteAuditLogs(DbContext context, DateTime now, string? userId)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IFullAuditable &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityKey  = string.Join(",",
                entry.Metadata.FindPrimaryKey()?.Properties
                    .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                ?? Enumerable.Empty<string?>());
            var action = entry.State.ToString();

            var properties = entry.State == EntityState.Added
                ? entry.Properties
                : entry.Properties.Where(p => p.IsModified);

            foreach (var prop in properties)
            {
                context.Set<AuditLog>().Add(new AuditLog
                {
                    EntityType   = entityType,
                    EntityKey    = entityKey,
                    PropertyName = prop.Metadata.Name,
                    OldValue     = entry.State == EntityState.Added   ? null : prop.OriginalValue?.ToString(),
                    NewValue     = entry.State == EntityState.Deleted  ? null : prop.CurrentValue?.ToString(),
                    Action       = action,
                    ChangedAt    = now,
                    ChangedBy    = userId
                });
            }
        }
    }
}
