namespace EfCoreKit.Sample.WebApi.Domain.Entities;

/// <summary>
/// Demonstrates the simplest base class <see cref="BaseEntity"/>:
/// just a typed primary key, no audit or soft-delete fields.
/// </summary>
public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<PostTag> PostTags { get; set; } = [];
}
