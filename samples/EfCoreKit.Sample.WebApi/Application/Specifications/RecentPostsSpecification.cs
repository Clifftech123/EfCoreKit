using EfCoreKit.Sample.WebApi.Domain.Entities;
using EfCoreKit.Specifications;

namespace EfCoreKit.Sample.WebApi.Application.Specifications;

/// <summary>
/// Returns posts created within the last <paramref name="days"/> days.
/// Demonstrates a parameterised specification.
/// </summary>
public sealed class RecentPostsSpecification : Specification<Post>
{
    public RecentPostsSpecification(int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        AddCriteria(p => p.CreatedAt >= cutoff);
        AddInclude("Category");
        ApplyOrderByDescending(p => (object)p.CreatedAt);
        ApplyAsNoTracking();
    }
}
