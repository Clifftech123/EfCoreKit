using EfCoreKit.Sample.WebApi.Domain.Entities;
using EfCoreKit.Specifications;

namespace EfCoreKit.Sample.WebApi.Application.Specifications;

/// <summary>
/// Returns only published posts, ordered newest-first, with Category eagerly loaded.
/// Demonstrates a reusable typed <see cref="Specification{T}"/> class.
/// </summary>
public sealed class PublishedPostsSpecification : Specification<Post>
{
    public PublishedPostsSpecification()
    {
        AddCriteria(p => p.IsPublished);
        AddInclude("Category");
        AddInclude("PostTags.Tag");
        ApplyOrderByDescending(p => (object)p.CreatedAt);
        ApplyAsNoTracking();
    }
}
