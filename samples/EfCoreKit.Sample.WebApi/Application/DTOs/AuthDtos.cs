namespace EfCoreKit.Sample.WebApi.Application.DTOs;

// ── Request DTOs ───────────────────────────────────────────────────────────────

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

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
    DateTime? LastLoginAt);
