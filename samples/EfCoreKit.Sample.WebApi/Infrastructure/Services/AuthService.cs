using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EfCoreKit.Sample.WebApi.Application.DTOs;
using EfCoreKit.Sample.WebApi.Application.Interfaces;
using EfCoreKit.Sample.WebApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Services;

public sealed class AuthService(
    UserManager<User> userManager,
    IConfiguration config) : IAuthService
{
    // ── Register ───────────────────────────────────────────────────────────────

    public async Task<CurrentUserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TenantId = request.TenantId,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(result.Errors.First().Description);

        return ToCurrentUserResponse(user);
    }

    // ── Login ──────────────────────────────────────────────────────────────────

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAccessException("Account is locked. Try again later.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var (accessToken, expiry) = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = HashToken(refreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return new LoginResponse(accessToken, refreshToken, expiry);
    }

    // ── Refresh token ──────────────────────────────────────────────────────────

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        // Validate the expired access token structurally — does NOT enforce expiry
        var principal = ValidateToken(request.AccessToken, validateLifetime: false);
        var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedAccessException("Invalid token.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (user.RefreshToken is null
            || user.RefreshTokenExpiry < DateTime.UtcNow
            || user.RefreshToken != HashToken(request.RefreshToken))
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        var (accessToken, expiry) = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = HashToken(newRefreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
        await userManager.UpdateAsync(user);

        return new TokenResponse(accessToken, newRefreshToken, expiry);
    }

    // ── Logout ─────────────────────────────────────────────────────────────────

    public async Task LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await userManager.UpdateAsync(user);
    }

    // ── Change password ────────────────────────────────────────────────────────

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        if (request.CurrentPassword == request.NewPassword)
            throw new InvalidOperationException("New password must differ from the current password.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(result.Errors.First().Description);

        // Revoke refresh token so all existing sessions must re-login
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await userManager.UpdateAsync(user);
    }

    // ── Get current user ───────────────────────────────────────────────────────

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        return ToCurrentUserResponse(user);
    }

    // ── JWT helpers ────────────────────────────────────────────────────────────

    private (string token, DateTime expiry) GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!));
        var expiry = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // tenant_id — read by HttpContextTenantProvider to scope every EfCoreKit query
            new Claim("tenant_id", user.TenantId)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    private ClaimsPrincipal ValidateToken(string token, bool validateLifetime)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            throw new UnauthorizedAccessException("Invalid token.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out var validated);

            // Reject algorithm substitution attacks (e.g. 'none', RS256 → HS256)
            if (validated is not JwtSecurityToken jwt
                || !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Invalid token.");

            return principal;
        }
        catch (SecurityTokenException)
        {
            throw new UnauthorizedAccessException("Invalid token.");
        }
    }

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private int AccessTokenExpiryMinutes =>
        int.Parse(config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

    private int RefreshTokenExpiryDays =>
        int.Parse(config["Jwt:RefreshTokenExpiryDays"] ?? "7");

    private static CurrentUserResponse ToCurrentUserResponse(User u) =>
        new(u.Id, u.FullName, u.Email!, u.TenantId, u.LastLoginAt);
}
