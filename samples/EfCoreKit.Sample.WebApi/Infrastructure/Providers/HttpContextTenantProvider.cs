using EfCoreKit.Interfaces;
using EfCoreKit.Sample.WebApi.Application.Interfaces;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Providers;

/// <summary>
/// EfCoreKit's <see cref="ITenantProvider"/> implementation.
/// Delegates to <see cref="ICurrentUserService"/> so the tenant_id claim is read
/// in exactly one place. EfCoreKit's TenantInterceptor calls this to stamp
/// TenantId on new entities and to apply the global query filter that scopes
/// all ITenantEntity queries to the current tenant.
/// </summary>
public sealed class HttpContextTenantProvider : ITenantProvider
{
    private readonly ICurrentUserService _currentUser;

    public HttpContextTenantProvider(ICurrentUserService currentUser)
        => _currentUser = currentUser;

    public string? GetCurrentTenantId() => _currentUser.GetTenantId();
}
