namespace EfCoreKit.Sample.WebApi.Domain.Entities;

/// <summary>
/// Demonstrates <see cref="SoftDeletableEntity"/>:
/// gets audit fields (CreatedAt/By, UpdatedAt/By) AND soft-delete fields (IsDeleted, DeletedAt/By)
/// automatically managed by EfCoreKit interceptors.
/// </summary>
public class Category : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Post> Posts { get; set; } = [];
}
