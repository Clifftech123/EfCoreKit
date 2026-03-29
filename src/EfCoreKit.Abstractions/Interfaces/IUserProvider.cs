namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Provides the current user information for audit trail support.
/// Implement this interface to resolve the user from your application context
/// (e.g., HTTP context claims, ambient identity, or a test stub).
/// </summary>
public interface IUserProvider
{
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// </summary>
    /// <returns>The current user ID, or <c>null</c> if no user is available.</returns>
    string? GetCurrentUserId();

    /// <summary>
    /// Gets the display name of the current user.
    /// </summary>
    /// <returns>The current user name, or <c>null</c> if no user is available.</returns>
    string? GetCurrentUserName();
}
