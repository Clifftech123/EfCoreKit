using EfCoreKit.Interfaces;
using EfCoreKit.Sample.WebApi.Application.Interfaces;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Providers;

/// <summary>
/// EfCoreKit's <see cref="IUserProvider"/> implementation.
/// Delegates to <see cref="ICurrentUserService"/> — the single place that reads
/// from HttpContext claims — so the audit interceptor (CreatedBy / UpdatedBy) and
/// the rest of the application always agree on who the current user is.
/// </summary>
public sealed class HttpContextUserProvider : IUserProvider
{
    private readonly ICurrentUserService _currentUser;

    public HttpContextUserProvider(ICurrentUserService currentUser)
        => _currentUser = currentUser;

    public string? GetCurrentUserId() => _currentUser.GetUserId()?.ToString();
    public string? GetCurrentUserName() => _currentUser.GetUserName();
}
