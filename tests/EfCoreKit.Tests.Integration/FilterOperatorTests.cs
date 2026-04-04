using EfCoreKit.Exceptions;
using EfCoreKit.Extensions;
using EfCoreKit.Models;

namespace EfCoreKit.Tests.Integration;

public class FilterOperatorTests
{
    // ── ne (not equal) ────────────────────────────────────────────────

    [Fact]
    public async Task ApplyFilters_NeOperator_ExcludesMatching()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Alpha", Price = 1m },
            new Product { Name = "Beta",  Price = 2m }
        );
        await ctx.SaveChangesAsync();

        var filters = new[] { new FilterDescriptor { Field = "Name", Operator = "ne", Value = "Alpha" } };
        var result = await ctx.Products.ApplyFilters(filters).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Beta", result[0].Name);
    }

    // ── gt (greater than) ─────────────────────────────────────────────

    [Fact]
    public async Task ApplyFilters_GtOperator_FiltersCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Low",  Price = 5m },
            new Product { Name = "High", Price = 50m }
        );
        await ctx.SaveChangesAsync();

        var filters = new[] { new FilterDescriptor { Field = "Price", Operator = "gt", Value = 10m } };
        var result = await ctx.Products.ApplyFilters(filters).ToListAsync();

        Assert.Single(result);
        Assert.Equal("High", result[0].Name);
    }

    // ── lt (less than) ────────────────────────────────────────────────

    [Fact]
    public async Task ApplyFilters_LtOperator_FiltersCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Low",  Price = 5m },
            new Product { Name = "High", Price = 50m }
        );
        await ctx.SaveChangesAsync();

        var filters = new[] { new FilterDescriptor { Field = "Price", Operator = "lt", Value = 10m } };
        var result = await ctx.Products.ApplyFilters(filters).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Low", result[0].Name);
    }

    // ── startswith ────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyFilters_StartsWithOperator_FiltersStrings()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "SuperWidget", Price = 1m },
            new Product { Name = "MegaGadget",  Price = 2m }
        );
        await ctx.SaveChangesAsync();

        var filters = new[] { new FilterDescriptor { Field = "Name", Operator = "startswith", Value = "Super" } };
        var result = await ctx.Products.ApplyFilters(filters).ToListAsync();

        Assert.Single(result);
        Assert.Equal("SuperWidget", result[0].Name);
    }

    // ── endswith ──────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyFilters_EndsWithOperator_FiltersStrings()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "SuperWidget", Price = 1m },
            new Product { Name = "MegaGadget",  Price = 2m }
        );
        await ctx.SaveChangesAsync();

        var filters = new[] { new FilterDescriptor { Field = "Name", Operator = "endswith", Value = "Gadget" } };
        var result = await ctx.Products.ApplyFilters(filters).ToListAsync();

        Assert.Single(result);
        Assert.Equal("MegaGadget", result[0].Name);
    }

    // ── in ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyFilters_InOperator_FiltersToMatchingValues()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 10m },
            new Product { Name = "B", Price = 20m },
            new Product { Name = "C", Price = 30m }
        );
        await ctx.SaveChangesAsync();

        var filters = new[]
        {
            new FilterDescriptor { Field = "Name", Operator = "in", Value = new List<string> { "A", "C" } }
        };
        var result = await ctx.Products.ApplyFilters(filters).OrderBy(p => p.Name).ToListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Name);
        Assert.Equal("C", result[1].Name);
    }

    [Fact]
    public async Task ApplyFilters_InOperator_ThrowsForNonEnumerableValue()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.Add(new Product { Name = "X", Price = 1m });
        await ctx.SaveChangesAsync();

        var filters = new[]
        {
            new FilterDescriptor { Field = "Name", Operator = "in", Value = "not-a-list" }
        };

        // string is IEnumerable<char> — this may not throw the way we expect.
        // The real intent is to verify non-IEnumerable throws.
        var nonEnumFilters = new[]
        {
            new FilterDescriptor { Field = "Price", Operator = "in", Value = 42 }
        };

        await Assert.ThrowsAsync<InvalidFilterException>(() =>
            ctx.Products.ApplyFilters(nonEnumFilters).ToListAsync());
    }

    // ── between ───────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyFilters_BetweenOperator_FiltersRange()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Cheap",     Price = 5m },
            new Product { Name = "Mid",       Price = 50m },
            new Product { Name = "Expensive", Price = 500m }
        );
        await ctx.SaveChangesAsync();

        var filters = new[]
        {
            new FilterDescriptor { Field = "Price", Operator = "between", Value = new object[] { 10m, 100m } }
        };
        var result = await ctx.Products.ApplyFilters(filters).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Mid", result[0].Name);
    }

    [Fact]
    public async Task ApplyFilters_BetweenOperator_ThrowsForInvalidValue()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.Add(new Product { Name = "X", Price = 1m });
        await ctx.SaveChangesAsync();

        // Not a 2-element array
        var filters = new[]
        {
            new FilterDescriptor { Field = "Price", Operator = "between", Value = new object[] { 1m } }
        };

        await Assert.ThrowsAsync<InvalidFilterException>(() =>
            ctx.Products.ApplyFilters(filters).ToListAsync());
    }

    // ── Empty filter field ────────────────────────────────────────────

    [Fact]
    public async Task ApplyFilters_EmptyFieldName_ThrowsInvalidFilterException()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.Add(new Product { Name = "X", Price = 1m });
        await ctx.SaveChangesAsync();

        var filters = new[] { new FilterDescriptor { Field = "", Operator = "eq", Value = "X" } };

        await Assert.ThrowsAsync<InvalidFilterException>(() =>
            ctx.Products.ApplyFilters(filters).ToListAsync());
    }

    // ── Null/empty filters collection ─────────────────────────────────

    [Fact]
    public async Task ApplyFilters_NullOrEmpty_ReturnsAllResults()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 1m },
            new Product { Name = "B", Price = 2m }
        );
        await ctx.SaveChangesAsync();

        var resultNull = await ctx.Products.ApplyFilters(null).ToListAsync();
        var resultEmpty = await ctx.Products.ApplyFilters([]).ToListAsync();

        Assert.Equal(2, resultNull.Count);
        Assert.Equal(2, resultEmpty.Count);
    }
}
