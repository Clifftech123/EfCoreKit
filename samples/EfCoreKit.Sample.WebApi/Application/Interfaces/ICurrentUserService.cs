namespace EfCoreKit.Sample.WebApi.Application.Interfaces;

/// <summary>
/// Single source of truth for the current authenticated user across the whole application.
///
/// EfCoreKit's <c>IUserProvider</c> delegates to this — so there is exactly one place
/// that reads from <c>HttpContext</c>, and everything else depends on this interface.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Returns the current user's ID (from the JWT sub claim), or null if unauthenticated.</summary>
    Guid? GetUserId();

    /// <summary>Returns the current user's display name (from the JWT name claim).</summary>
    string? GetUserName();

    /// <summary>Whether the current request is authenticated.</summary>
    bool IsAuthenticated { get; }
}
