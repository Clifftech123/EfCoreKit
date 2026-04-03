using EfCoreKit.Abstractions.Interfaces;
using EfCoreKit.Core.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreKit.Core.Interceptors;

/// <summary>
/// Interceptor that automatically sets <see cref="IAuditable"/> properties
/// (<c>CreatedAt</c>, <c>CreatedBy</c>, <c>UpdatedAt</c>, <c>UpdatedBy</c>)
/// on save.
/// </summary>
/// <remarks>
/// <para>
/// On <see cref="EntityState.Added"/>: sets <c>CreatedAt</c> and <c>CreatedBy</c>.
/// On <see cref="EntityState.Modified"/>: sets <c>UpdatedAt</c> and <c>UpdatedBy</c>,
/// and protects <c>CreatedAt</c>/<c>CreatedBy</c> from being overwritten.
/// </para>
/// <para>
/// Both sync (<c>SaveChanges</c>) and async (<c>SaveChangesAsync</c>) paths are handled.
/// </para>
/// </remarks>
/// <example>
/// Register standalone (without <see cref="EfCoreKitDbContext{TContext}"/>):
/// <code>
/// services.AddDbContext&lt;MyDbContext&gt;((sp, options) =&gt;
/// {
///     options.UseSqlServer(connectionString)
///            .AddInterceptors(new AuditInterceptor(
///                sp.GetRequiredService&lt;EfCoreKitOptions&gt;(),
///                sp.GetService&lt;IUserProvider&gt;()));
/// });
/// </code>
/// </example>
internal sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly EfCoreKitOptions _options;
    private readonly IUserProvider? _userProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditInterceptor"/> class.
    /// </summary>
    /// <param name="options">The EfCoreKit configuration options.</param>
    /// <param name="userProvider">The user provider for resolving the current user.</param>
    public AuditInterceptor(EfCoreKitOptions options, IUserProvider? userProvider = null)
    {
        _options = options;
        _userProvider = userProvider;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditFields(DbContext? context)
    {
        if (context is null || !_options.AuditTrailEnabled)
            return;

        var now = DateTime.UtcNow;
        var userId = _userProvider?.GetCurrentUserId();

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
                    // Protect CreatedAt/CreatedBy from being overwritten on update
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                    break;
            }
        }
    }
}
