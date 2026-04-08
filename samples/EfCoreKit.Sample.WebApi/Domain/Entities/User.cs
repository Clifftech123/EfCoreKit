using Microsoft.AspNetCore.Identity;

namespace EfCoreKit.Sample.WebApi.Domain.Entities;

/// <summary>
/// Application user. Extends IdentityUser&lt;Guid&gt; with profile fields.
/// EfCoreKit's AuditInterceptor reads the user claim and automatically stamps
/// audit fields on every entity change — no manual assignment needed.
/// </summary>
public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

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
