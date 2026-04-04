using EfCoreKit.Exceptions;
using EfCoreKit.Extensions;
using EfCoreKit.Models;

namespace EfCoreKit.Tests.Integration;

public class PaginationAndFilterTests
{
    // ── Pagination ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ToPagedAsync_FirstPage_ReturnsCorrectSlice()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(Enumerable.Range(1, 7)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await ctx.SaveChangesAsync();

        var result = await ctx.Products.OrderBy(p => p.Id).ToPagedAsync(page: 1, pageSize: 3);

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task ToPagedAsync_SecondPage_SkipsFirstPage()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(Enumerable.Range(1, 5)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await ctx.SaveChangesAsync();

        var page1 = await ctx.Products.OrderBy(p => p.Id).ToPagedAsync(1, 3);
        var page2 = await ctx.Products.OrderBy(p => p.Id).ToPagedAsync(2, 3);

        Assert.Equal(3, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.DoesNotContain(page2.Items, p => page1.Items.Select(x => x.Id).Contains(p.Id));
    }

    [Fact]
    public async Task ToPagedAsync_ThrowsArgumentOutOfRange_ForInvalidInputs()
    {
        await using var ctx = DbFactory.CreateBasic();
        var query = ctx.Products.AsQueryable();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => query.ToPagedAsync(0, 10));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => query.ToPagedAsync(1, 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => query.ToPagedAsync(1, 1001));
    }

    // ── Dynamic filters ───────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyFilters_EqOperator_FiltersCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Widget", Price = 10m, IsActive = true  },
            new Product { Name = "Gadget", Price = 20m, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var filters = new[] { new FilterDescriptor { Field = "IsActive", Operator = "eq", Value = true } };
        var result  = await ctx.Products.ApplyFilters(filters).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Widget", result[0].Name);
    }

    [Fact]
    public async Task ApplyFilters_ContainsOperator_FiltersStrings()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "SuperWidget",  Price = 1m },
            new Product { Name = "NormalGadget", Price = 2m }
        );
        await ctx.SaveChangesAsync();

        var filters = new[] { new FilterDescriptor { Field = "Name", Operator = "contains", Value = "Widget" } };
        var result  = await ctx.Products.ApplyFilters(filters).ToListAsync();

        Assert.Single(result);
        Assert.Equal("SuperWidget", result[0].Name);
    }

    [Fact]
    public async Task ApplyFilters_GteAndLte_RangeFilter()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Cheap",     Price = 5m   },
            new Product { Name = "Mid",       Price = 50m  },
            new Product { Name = "Expensive", Price = 500m }
        );
        await ctx.SaveChangesAsync();

        var filters = new[]
        {
            new FilterDescriptor { Field = "Price", Operator = "gte", Value = 10m  },
            new FilterDescriptor { Field = "Price", Operator = "lte", Value = 100m }
        };
        var result = await ctx.Products.ApplyFilters(filters).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Mid", result[0].Name);
    }

    [Fact]
    public async Task ApplyFilters_InvalidOperator_ThrowsInvalidFilterException()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.Add(new Product { Name = "X", Price = 1m });
        await ctx.SaveChangesAsync();

        var filters = new[] { new FilterDescriptor { Field = "Name", Operator = "unknown", Value = "X" } };

        await Assert.ThrowsAsync<InvalidFilterException>(() =>
            ctx.Products.ApplyFilters(filters).ToListAsync());
    }

    // ── Dynamic sorts ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplySorts_SingleField_OrdersCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Z", Price = 30m },
            new Product { Name = "A", Price = 10m },
            new Product { Name = "M", Price = 20m }
        );
        await ctx.SaveChangesAsync();

        var sorts  = new[] { new SortDescriptor { Field = "Price", Ascending = true } };
        var result = await ctx.Products.ApplySorts(sorts).ToListAsync();

        Assert.Equal(new[] { "A", "M", "Z" }, result.Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task WhereIf_AppliesFilter_WhenConditionIsTrue()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Active",   IsActive = true  },
            new Product { Name = "Inactive", IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var result = await ctx.Products.WhereIf(true, p => p.IsActive).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task WhereIf_SkipsFilter_WhenConditionIsFalse()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Active",   IsActive = true  },
            new Product { Name = "Inactive", IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var result = await ctx.Products.WhereIf(false, p => p.IsActive).ToListAsync();

        Assert.Equal(2, result.Count);
    }
}
