namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Provides the current tenant identifier for multi-tenancy support.
/// Implement this interface to resolve the tenant from your application context
/// (e.g., HTTP headers, claims, or route data).
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the tenant identifier for the current request or scope.
    /// </summary>
    /// <returns>The current tenant identifier, or <c>null</c> if no tenant is available.</returns>
    string? GetCurrentTenantId();
}
