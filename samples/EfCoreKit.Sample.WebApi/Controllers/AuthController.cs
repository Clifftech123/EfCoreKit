using System.Security.Claims;
using EfCoreKit.Sample.WebApi.Application.DTOs;
using EfCoreKit.Sample.WebApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace EfCoreKit.Sample.WebApi.Controllers;

/// <summary>
/// Handles registration, login, token refresh, logout, password change.
///
/// Flow for multi-tenant access to Posts:
///   1. POST /api/auth/register  — create a user and assign them a TenantId
///   2. POST /api/auth/login     — get back an access token + refresh token
///   3. Access token carries "tenant_id" claim → HttpContextTenantProvider reads it
///   4. Every call to PostsController is automatically scoped to that tenant
///      by EfCoreKit's TenantInterceptor + global query filter — no manual filtering needed
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var user = await _auth.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(Me), user);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        return Ok(result);
    }

    // POST /api/auth/refresh
    // Exchange an expired access token + valid refresh token for a new pair.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _auth.RefreshTokenAsync(request, ct);
        return Ok(result);
    }

    // POST /api/auth/logout
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _auth.LogoutAsync(userId, ct);
        return NoContent();
    }

    // POST /api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _auth.ChangePasswordAsync(userId, request, ct);
        return NoContent();
    }

    // GET /api/auth/me
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _auth.GetCurrentUserAsync(userId, ct);
        return Ok(user);
    }
}
