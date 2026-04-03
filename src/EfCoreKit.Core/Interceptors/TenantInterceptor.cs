using EfCoreKit.Abstractions.Exceptions;
using EfCoreKit.Abstractions.Interfaces;
using EfCoreKit.Core.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreKit.Core.Interceptors;

/// <summary>
/// Interceptor that automatically assigns the current tenant ID to entities
/// implementing <see cref="ITenantEntity"/> on insert, and validates tenant
/// ownership on update.
/// </summary>
/// <remarks>
/// <para>
/// On <see cref="EntityState.Added"/>: assigns the current tenant ID from
/// <see cref="ITenantProvider.GetCurrentTenantId"/> if the entity's <c>TenantId</c> is empty.
/// </para>
/// <para>
/// On <see cref="EntityState.Modified"/>: validates that the entity's original <c>TenantId</c>
/// matches the current tenant. Throws <see cref="TenantMismatchException"/> on mismatch
/// and prevents <c>TenantId</c> from being changed.
/// </para>
/// </remarks>
/// <example>
/// Register standalone (without <see cref="EfCoreKitDbContext{TContext}"/>):
/// <code>
/// services.AddDbContext&lt;MyDbContext&gt;((sp, options) =&gt;
/// {
///     options.UseSqlServer(connectionString)
///            .AddInterceptors(new TenantInterceptor(
///                sp.GetRequiredService&lt;EfCoreKitOptions&gt;(),
///                sp.GetService&lt;ITenantProvider&gt;()));
/// });
/// </code>
/// </example>
internal sealed class TenantInterceptor : SaveChangesInterceptor
{
    private readonly EfCoreKitOptions _options;
    private readonly ITenantProvider? _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantInterceptor"/> class.
    /// </summary>
    /// <param name="options">The EfCoreKit configuration options.</param>
    /// <param name="tenantProvider">The tenant provider for resolving the current tenant.</param>
    public TenantInterceptor(EfCoreKitOptions options, ITenantProvider? tenantProvider = null)
    {
        _options = options;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EnforceTenancy(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnforceTenancy(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void EnforceTenancy(DbContext? context)
    {
        if (context is null || !_options.MultiTenancyEnabled)
            return;

        var currentTenantId = _tenantProvider?.GetCurrentTenantId();

        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (string.IsNullOrEmpty(entry.Entity.TenantId))
                    {
                        entry.Entity.TenantId = currentTenantId;
                    }
                    break;

                case EntityState.Modified:
                    var originalTenantId = entry.OriginalValues.GetValue<string?>(nameof(ITenantEntity.TenantId));
                    if (!string.IsNullOrEmpty(currentTenantId)
                        && !string.Equals(originalTenantId, currentTenantId, StringComparison.Ordinal))
                    {
                        throw new TenantMismatchException(currentTenantId, originalTenantId);
                    }
                    // Prevent TenantId from being changed
                    entry.Property(nameof(ITenantEntity.TenantId)).IsModified = false;
                    break;
            }
        }
    }
}
