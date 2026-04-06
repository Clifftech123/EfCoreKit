using System.Security.Claims;
using EfCoreKit.Sample.WebApi.Application.Interfaces;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Services;

/// <summary>
/// Reads the current user's identity from the HTTP context claims.
/// Registered as scoped — one instance per HTTP request.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public bool IsAuthenticated
        => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? GetUserId()
    {
        var value = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public string? GetUserName()
        => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public string? GetTenantId()
        => _httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
}
