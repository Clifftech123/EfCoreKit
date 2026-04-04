using EfCoreKit.Extensions;
using EfCoreKit.Specifications;

namespace EfCoreKit.Tests.Integration;

// ── Concrete spec classes ─────────────────────────────────────────────────────

internal sealed class ActiveProductsSpec : Specification<Product>
{
    public ActiveProductsSpec() => AddCriteria(p => p.IsActive);
}

internal sealed class ExpensiveProductsSpec : Specification<Product>
{
    public ExpensiveProductsSpec(decimal threshold) =>
        AddCriteria(p => p.Price >= threshold);
}

internal sealed class OrderedProductsSpec : Specification<Product>
{
    public OrderedProductsSpec()
    {
        AddCriteria(p => p.IsActive);
        ApplyOrderBy(p => (object)p.Price);
        ApplyPaging(skip: 0, take: 2);
    }
}

internal sealed class ProductNameSpec : Specification<Product, string>
{
    public ProductNameSpec(bool onlyActive)
    {
        if (onlyActive) AddCriteria(p => p.IsActive);
        ApplySelector(p => p.Name);
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class SpecificationTests
{
    [Fact]
    public async Task ApplySpecification_Criteria_FiltersResults()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Active",   Price = 5m,  IsActive = true  },
            new Product { Name = "Inactive", Price = 10m, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var results = await ctx.Products
            .ApplySpecification(new ActiveProductsSpec())
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("Active", results[0].Name);
    }

    [Fact]
    public async Task ApplySpecification_OrderByAndTake_AppliesPaging()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "P1", Price = 30m, IsActive = true },
            new Product { Name = "P2", Price = 10m, IsActive = true },
            new Product { Name = "P3", Price = 20m, IsActive = true }
        );
        await ctx.SaveChangesAsync();

        var results = await ctx.Products
            .ApplySpecification(new OrderedProductsSpec())
            .ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal("P2", results[0].Name);  // cheapest first
        Assert.Equal("P3", results[1].Name);
    }

    [Fact]
    public async Task AndCombinator_IntersectsBothSpecs()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Active Cheap",     Price = 5m,  IsActive = true  },
            new Product { Name = "Active Expensive", Price = 50m, IsActive = true  },
            new Product { Name = "Inactive Cheap",   Price = 5m,  IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var spec = new ActiveProductsSpec().And(new ExpensiveProductsSpec(20m));
        var results = await ctx.Products.ApplySpecification(spec).ToListAsync();

        Assert.Single(results);
        Assert.Equal("Active Expensive", results[0].Name);
    }

    [Fact]
    public async Task OrCombinator_UnionsBothSpecs()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Active",    Price = 5m,  IsActive = true  },
            new Product { Name = "Inactive",  Price = 5m,  IsActive = false },
            new Product { Name = "Expensive", Price = 99m, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var spec = new ActiveProductsSpec().Or(new ExpensiveProductsSpec(50m));
        var results = await ctx.Products.ApplySpecification(spec).ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, p => p.Name == "Active");
        Assert.Contains(results, p => p.Name == "Expensive");
    }

    [Fact]
    public async Task ProjectingSpecification_ReturnsMappedType()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Alpha", Price = 1m, IsActive = true  },
            new Product { Name = "Beta",  Price = 2m, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var names = await ctx.Products.ToListAsync(new ProductNameSpec(onlyActive: true));

        Assert.Equal(["Alpha"], names);
    }
}
