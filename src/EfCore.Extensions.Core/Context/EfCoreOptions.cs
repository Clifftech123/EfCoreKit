using EfCore.Extensions.Abstractions.Interfaces;

namespace EfCore.Extensions.Core.Context;

/// <summary>
/// Configuration options for EfCore.Extensions features.
/// Use the fluent methods to enable features before passing to <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class EfCoreOptions
{
    /// <summary>
    /// Gets a value indicating whether soft delete is enabled for entities implementing <see cref="ISoftDeletable"/>.
    /// </summary>
    public bool SoftDeleteEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether cascade soft delete is enabled.
    /// </summary>
    public bool CascadeSoftDeleteEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether audit trail is enabled for entities implementing <see cref="IAuditable"/>.
    /// </summary>
    public bool AuditTrailEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether full audit logging to a dedicated table is enabled.
    /// </summary>
    public bool FullAuditLogEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether multi-tenancy is enabled for entities implementing <see cref="ITenantEntity"/>.
    /// </summary>
    public bool MultiTenancyEnabled { get; private set; }

    /// <summary>
    /// Gets the type implementing <see cref="IUserProvider"/>.
    /// </summary>
    public Type? UserProviderType { get; private set; }

    /// <summary>
    /// Gets the type implementing <see cref="ITenantProvider"/>.
    /// </summary>
    public Type? TenantProviderType { get; private set; }

    /// <summary>
    /// Gets the threshold for logging slow queries. <c>null</c> disables slow query logging.
    /// </summary>
    public TimeSpan? SlowQueryThreshold { get; private set; }

    /// <summary>
    /// Enables soft delete for entities implementing <see cref="ISoftDeletable"/>.
    /// </summary>
    /// <param name="cascade">Whether to enable cascade soft delete for related entities.</param>
    /// <returns>This instance for chaining.</returns>
    public EfCoreOptions EnableSoftDelete(bool cascade = false)
    {
        SoftDeleteEnabled = true;
        CascadeSoftDeleteEnabled = cascade;
        return this;
    }

    /// <summary>
    /// Enables audit trail for entities implementing <see cref="IAuditable"/>.
    /// </summary>
    /// <param name="fullLog">Whether to enable full change logging to a dedicated audit table.</param>
    /// <returns>This instance for chaining.</returns>
    public EfCoreOptions EnableAuditTrail(bool fullLog = false)
    {
        AuditTrailEnabled = true;
        FullAuditLogEnabled = fullLog;
        return this;
    }

    /// <summary>
    /// Enables multi-tenancy for entities implementing <see cref="ITenantEntity"/>.
    /// </summary>
    /// <returns>This instance for chaining.</returns>
    public EfCoreOptions EnableMultiTenancy()
    {
        MultiTenancyEnabled = true;
        return this;
    }

    /// <summary>
    /// Registers the <see cref="IUserProvider"/> implementation to use for audit trail.
    /// </summary>
    /// <typeparam name="T">The concrete type implementing <see cref="IUserProvider"/>.</typeparam>
    /// <returns>This instance for chaining.</returns>
    public EfCoreOptions UseUserProvider<T>() where T : class, IUserProvider
    {
        UserProviderType = typeof(T);
        return this;
    }

    /// <summary>
    /// Registers the <see cref="ITenantProvider"/> implementation to use for multi-tenancy.
    /// </summary>
    /// <typeparam name="T">The concrete type implementing <see cref="ITenantProvider"/>.</typeparam>
    /// <returns>This instance for chaining.</returns>
    public EfCoreOptions UseTenantProvider<T>() where T : class, ITenantProvider
    {
        TenantProviderType = typeof(T);
        return this;
    }

    /// <summary>
    /// Enables slow query logging for queries exceeding the given threshold.
    /// </summary>
    /// <param name="threshold">The duration threshold.</param>
    /// <returns>This instance for chaining.</returns>
    public EfCoreOptions LogSlowQueries(TimeSpan threshold)
    {
        SlowQueryThreshold = threshold;
        return this;
    }
}
