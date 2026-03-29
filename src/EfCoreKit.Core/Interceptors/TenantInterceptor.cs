using EfCoreKit.Abstractions.Interfaces;
using EfCoreKit.Core.Context;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreKit.Core.Interceptors;

/// <summary>
/// Interceptor that automatically assigns the current tenant ID to entities
/// implementing <see cref="ITenantEntity"/> on insert, and validates tenant
/// ownership on update.
/// </summary>
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
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // TODO: Iterate ChangeTracker entries implementing ITenantEntity
        // - For Added: set TenantId from _tenantProvider
        // - For Modified: validate TenantId matches current tenant, throw TenantMismatchException if not
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
