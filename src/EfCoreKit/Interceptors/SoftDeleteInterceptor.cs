using EfCoreKit.Interfaces;
using EfCoreKit.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreKit.Interceptors;

/// <summary>
/// Interceptor that converts physical deletes to soft deletes for entities
/// implementing <see cref="ISoftDeletable"/>.
/// </summary>
/// <remarks>
/// <para>
/// When an entity in <see cref="EntityState.Deleted"/> state implements <see cref="ISoftDeletable"/>,
/// the state is changed to <see cref="EntityState.Modified"/> and <c>IsDeleted</c>, <c>DeletedAt</c>,
/// and <c>DeletedBy</c> are set.
/// </para>
/// <para>
/// When <see cref="EfCoreOptions.CascadeSoftDeleteEnabled"/> is <c>true</c>, loaded child
/// navigation properties that implement <see cref="ISoftDeletable"/> are also soft-deleted.
/// </para>
/// </remarks>
/// <example>
/// Register standalone (without <see cref="EfCoreDbContext{TContext}"/>):
/// <code>
/// services.AddDbContext&lt;MyDbContext&gt;((sp, options) =&gt;
/// {
///     options.UseSqlServer(connectionString)
///            .AddInterceptors(new SoftDeleteInterceptor(
///                sp.GetRequiredService&lt;EfCoreOptions&gt;(),
///                sp.GetService&lt;IUserProvider&gt;()));
/// });
/// </code>
/// </example>
internal sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly EfCoreOptions _options;
    private readonly IUserProvider? _userProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteInterceptor"/> class.
    /// </summary>
    /// <param name="options">The EfCoreKit configuration options.</param>
    /// <param name="userProvider">The user provider for resolving who performed the delete.</param>
    public SoftDeleteInterceptor(EfCoreOptions options, IUserProvider? userProvider = null)
    {
        _options = options;
        _userProvider = userProvider;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        InterceptDeletes(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        InterceptDeletes(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void InterceptDeletes(DbContext? context)
    {
        if (context is null || !_options.SoftDeleteEnabled)
            return;

        var now = DateTime.UtcNow;
        var userId = _userProvider?.GetCurrentUserId();

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList())
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
            entry.Entity.DeletedBy = userId;

            // Cascade soft delete to loaded navigation properties
            if (_options.CascadeSoftDeleteEnabled)
            {
                CascadeSoftDelete(context, entry.Entity, now, userId);
            }
        }
    }

    private static void CascadeSoftDelete(DbContext context, ISoftDeletable parent, DateTime now, string? userId)
    {
        var parentEntry = context.Entry(parent);

        foreach (var navigation in parentEntry.Navigations)
        {
            if (navigation.CurrentValue is null)
                continue;

            if (navigation.CurrentValue is IEnumerable<ISoftDeletable> children)
            {
                foreach (var child in children)
                {
                    if (child.IsDeleted) continue;
                    child.IsDeleted = true;
                    child.DeletedAt = now;
                    child.DeletedBy = userId;
                    context.Entry(child).State = EntityState.Modified;
                }
            }
            else if (navigation.CurrentValue is ISoftDeletable child && !child.IsDeleted)
            {
                child.IsDeleted = true;
                child.DeletedAt = now;
                child.DeletedBy = userId;
                context.Entry(child).State = EntityState.Modified;
            }
        }
    }
}
