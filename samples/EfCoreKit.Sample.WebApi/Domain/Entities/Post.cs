namespace EfCoreKit.Sample.WebApi.Domain.Entities;

/// <summary>
/// Demonstrates <see cref="FullEntity"/>: audit + soft-delete + concurrency.
/// Belongs to a <see cref="User"/> (author) and a <see cref="Category"/>.
/// </summary>
public class Post : FullEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsPublished { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // The user who authored this post
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<PostTag> PostTags { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
}
