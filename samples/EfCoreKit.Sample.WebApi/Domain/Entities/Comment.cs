namespace EfCoreKit.Sample.WebApi.Domain.Entities;

/// <summary>
/// Demonstrates <see cref="AuditableEntity"/>: audit fields only, no soft-delete.
/// A comment belongs to a <see cref="Post"/> and is written by a <see cref="User"/>.
/// </summary>
public class Comment : AuditableEntity
{
    public string Body { get; set; } = string.Empty;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    // The authenticated user who wrote this comment
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
