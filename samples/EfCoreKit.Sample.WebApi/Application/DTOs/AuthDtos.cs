namespace EfCoreKit.Sample.WebApi.Application.DTOs;

// ── Request DTOs ───────────────────────────────────────────────────────────────

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    /// <summary>
    /// The tenant this user belongs to.
    /// All Posts created by this user will be scoped to this tenant via EfCoreKit's
    /// TenantInterceptor — no manual filtering required.
    /// </summary>
    string TenantId);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string AccessToken, string RefreshToken);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

// ── Response DTOs ──────────────────────────────────────────────────────────────

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public sealed record CurrentUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string TenantId,
    DateTime? LastLoginAt);
