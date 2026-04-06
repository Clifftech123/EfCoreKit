using EfCoreKit.Sample.WebApi.Domain.Entities;
using EfCoreKit.Specifications;

namespace EfCoreKit.Sample.WebApi.Application.Specifications;

/// <summary>
/// Filters posts by category ID.
/// Can be combined with <see cref="PublishedPostsSpecification"/> using the
/// <c>.And()</c> combinator: <c>new PublishedPostsSpecification().And(new PostsByCategorySpecification(id))</c>
/// </summary>
public sealed class PostsByCategorySpecification : Specification<Post>
{
    public PostsByCategorySpecification(int categoryId)
    {
        AddCriteria(p => p.CategoryId == categoryId);
        AddInclude("Category");
        ApplyOrderByDescending(p => (object)p.CreatedAt);
        ApplyAsNoTracking();
    }
}
