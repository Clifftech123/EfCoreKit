namespace EfCoreKit.Sample.WebApi.Domain.Entities;

/// <summary>
/// Many-to-many join table between <see cref="Post"/> and <see cref="Tag"/>.
/// </summary>
public class PostTag
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
