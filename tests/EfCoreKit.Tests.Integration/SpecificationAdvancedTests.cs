using EfCoreKit.Extensions;
using EfCoreKit.Specifications;

namespace EfCoreKit.Tests.Integration;

// ── Additional specification classes ──────────────────────────────────────────

internal sealed class OrderByDescendingSpec : Specification<Product>
{
    public OrderByDescendingSpec()
    {
        AddCriteria(p => p.IsActive);
        ApplyOrderByDescending(p => (object)p.Price);
    }
}

internal sealed class ThenBySortSpec : Specification<Product>
{
    public ThenBySortSpec()
    {
        ApplyOrderBy(p => (object)p.IsActive);
        ApplyThenBy(p => (object)p.Price);
    }
}

internal sealed class ThenByDescendingSortSpec : Specification<Product>
{
    public ThenByDescendingSortSpec()
    {
        ApplyOrderBy(p => (object)p.IsActive);
        ApplyThenByDescending(p => (object)p.Price);
    }
}

internal sealed class NoTrackingSpec : Specification<Product>
{
    public NoTrackingSpec()
    {
        AddCriteria(p => p.IsActive);
        ApplyAsNoTracking();
    }
}

internal sealed class PagingSpec : Specification<Product>
{
    public PagingSpec(int skip, int take)
    {
        ApplyOrderBy(p => (object)p.Id);
        ApplyPaging(skip, take);
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class SpecificationAdvancedTests
{
    [Fact]
    public async Task OrderByDescending_SortsCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Low",  Price = 10m, IsActive = true },
            new Product { Name = "High", Price = 50m, IsActive = true },
            new Product { Name = "Mid",  Price = 30m, IsActive = true }
        );
        await ctx.SaveChangesAsync();

        var results = await ctx.Products
            .ApplySpecification(new OrderByDescendingSpec())
            .ToListAsync();

        Assert.Equal(3, results.Count);
        Assert.Equal("High", results[0].Name);
        Assert.Equal("Mid",  results[1].Name);
        Assert.Equal("Low",  results[2].Name);
    }

    [Fact]
    public async Task ThenBy_AppliesSecondarySort()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "B", Price = 20m, IsActive = false },
            new Product { Name = "A", Price = 10m, IsActive = false },
            new Product { Name = "C", Price = 30m, IsActive = true }
        );
        await ctx.SaveChangesAsync();

        var results = await ctx.Products
            .ApplySpecification(new ThenBySortSpec())
            .ToListAsync();

        // false (0) before true (1), then by Price ascending within group
        Assert.Equal("A", results[0].Name);
        Assert.Equal("B", results[1].Name);
        Assert.Equal("C", results[2].Name);
    }

    [Fact]
    public async Task ThenByDescending_AppliesSecondarySortDescending()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "B", Price = 20m, IsActive = false },
            new Product { Name = "A", Price = 10m, IsActive = false },
            new Product { Name = "C", Price = 30m, IsActive = true }
        );
        await ctx.SaveChangesAsync();

        var results = await ctx.Products
            .ApplySpecification(new ThenByDescendingSortSpec())
            .ToListAsync();

        // false (0) before true (1), then by Price descending within group
        Assert.Equal("B", results[0].Name);
        Assert.Equal("A", results[1].Name);
        Assert.Equal("C", results[2].Name);
    }

    [Fact]
    public async Task AsNoTracking_EntitiesAreNotTracked()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.Add(new Product { Name = "NoTrack", Price = 1m, IsActive = true });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var results = await ctx.Products
            .ApplySpecification(new NoTrackingSpec())
            .ToListAsync();

        Assert.Single(results);
        Assert.Empty(ctx.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Paging_SkipsAndTakes()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(Enumerable.Range(1, 10)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await ctx.SaveChangesAsync();

        var results = await ctx.Products
            .ApplySpecification(new PagingSpec(skip: 3, take: 2))
            .ToListAsync();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ToPagedFromSpecAsync_CombinesSpecAndPagination()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Active1", Price = 10m, IsActive = true },
            new Product { Name = "Active2", Price = 20m, IsActive = true },
            new Product { Name = "Active3", Price = 30m, IsActive = true },
            new Product { Name = "Inactive", Price = 5m, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var page = await ctx.Products
            .ToPagedFromSpecAsync(new ActiveProductsSpec(), page: 1, pageSize: 2);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.TotalPages);
    }
}
