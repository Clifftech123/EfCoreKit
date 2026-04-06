using Microsoft.AspNetCore.Identity;

namespace EfCoreKit.Sample.WebApi.Domain.Entities;

/// <summary>
/// Application user. Extends IdentityUser&lt;Guid&gt; with profile fields and
/// a <c>TenantId</c> that is encoded into every JWT this user receives.
/// EfCoreKit's TenantInterceptor reads that claim and automatically scopes
/// every <see cref="Post"/> query to the correct tenant — no manual filtering needed.
/// </summary>
public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Encoded as the <c>tenant_id</c> JWT claim.</summary>
    public string TenantId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>SHA-256 hash of the refresh token — raw token is never persisted.</summary>
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigation — all posts and comments authored by this user
    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
}
