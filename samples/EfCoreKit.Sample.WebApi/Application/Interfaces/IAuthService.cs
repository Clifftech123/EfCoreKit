using EfCoreKit.Sample.WebApi.Application.DTOs;

namespace EfCoreKit.Sample.WebApi.Application.Interfaces;

public interface IAuthService
{
    Task<CurrentUserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(Guid userId, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
