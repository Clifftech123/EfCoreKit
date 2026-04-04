using EfCoreKit.Extensions;
using EfCoreKit.Models;

namespace EfCoreKit.Tests.Integration;

public class ConditionalFilterAndSortTests
{
    // ── WhereIfNotNull ────────────────────────────────────────────────

    [Fact]
    public async Task WhereIfNotNull_AppliesFilter_WhenValueNotNull()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Active",   IsActive = true, Price = 1m },
            new Product { Name = "Inactive", IsActive = false, Price = 2m }
        );
        await ctx.SaveChangesAsync();

        int? minPrice = 2;
        var result = await ctx.Products
            .WhereIfNotNull(minPrice, p => p.Price >= minPrice!.Value)
            .ToListAsync();

        Assert.Single(result);
        Assert.Equal("Inactive", result[0].Name);
    }

    [Fact]
    public async Task WhereIfNotNull_SkipsFilter_WhenValueIsNull()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 1m },
            new Product { Name = "B", Price = 2m }
        );
        await ctx.SaveChangesAsync();

        int? minPrice = null;
        var result = await ctx.Products
            .WhereIfNotNull(minPrice, p => p.Price >= 100)
            .ToListAsync();

        Assert.Equal(2, result.Count);
    }

    // ── WhereIfNotEmpty ───────────────────────────────────────────────

    [Fact]
    public async Task WhereIfNotEmpty_AppliesFilter_WhenValueNotEmpty()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Widget", Price = 1m },
            new Product { Name = "Gadget", Price = 2m }
        );
        await ctx.SaveChangesAsync();

        string? search = "Widget";
        var result = await ctx.Products
            .WhereIfNotEmpty(search, p => p.Name.Contains(search!))
            .ToListAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task WhereIfNotEmpty_SkipsFilter_WhenValueIsNullOrWhitespace()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 1m },
            new Product { Name = "B", Price = 2m }
        );
        await ctx.SaveChangesAsync();

        string? nullSearch = null;
        var resultNull = await ctx.Products
            .WhereIfNotEmpty(nullSearch, p => p.Name == "nope")
            .ToListAsync();

        string emptySearch = "   ";
        var resultEmpty = await ctx.Products
            .WhereIfNotEmpty(emptySearch, p => p.Name == "nope")
            .ToListAsync();

        Assert.Equal(2, resultNull.Count);
        Assert.Equal(2, resultEmpty.Count);
    }

    // ── Multi-column ApplySorts ───────────────────────────────────────

    [Fact]
    public async Task ApplySorts_MultiColumn_SortsCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 20m, IsActive = true },
            new Product { Name = "B", Price = 10m, IsActive = true },
            new Product { Name = "C", Price = 10m, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var sorts = new[]
        {
            new SortDescriptor { Field = "Price", Ascending = true },
            new SortDescriptor { Field = "Name",  Ascending = false }
        };
        var result = await ctx.Products.ApplySorts(sorts).ToListAsync();

        // Price ascending, then Name descending within same price
        Assert.Equal("C", result[0].Name); // Price=10, Name=C
        Assert.Equal("B", result[1].Name); // Price=10, Name=B
        Assert.Equal("A", result[2].Name); // Price=20
    }

    [Fact]
    public async Task ApplySorts_Descending_SortsCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 10m },
            new Product { Name = "B", Price = 30m },
            new Product { Name = "C", Price = 20m }
        );
        await ctx.SaveChangesAsync();

        var sorts = new[] { new SortDescriptor { Field = "Price", Ascending = false } };
        var result = await ctx.Products.ApplySorts(sorts).ToListAsync();

        Assert.Equal(new[] { "B", "C", "A" }, result.Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task ApplySorts_NullOrEmpty_ReturnsUnchanged()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 1m },
            new Product { Name = "B", Price = 2m }
        );
        await ctx.SaveChangesAsync();

        var resultNull = await ctx.Products.ApplySorts(null).ToListAsync();
        var resultEmpty = await ctx.Products.ApplySorts([]).ToListAsync();

        Assert.Equal(2, resultNull.Count);
        Assert.Equal(2, resultEmpty.Count);
    }

    // ── OrderByDynamic ────────────────────────────────────────────────

    [Fact]
    public async Task OrderByDynamic_Descending_Works()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 10m },
            new Product { Name = "B", Price = 30m }
        );
        await ctx.SaveChangesAsync();

        var result = await ctx.Products.OrderByDynamic("Price", ascending: false).ToListAsync();

        Assert.Equal("B", result[0].Name);
        Assert.Equal("A", result[1].Name);
    }

    // ── Projection extensions ─────────────────────────────────────────

    [Fact]
    public async Task SelectToListAsync_ProjectsCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Alpha", Price = 10m },
            new Product { Name = "Beta",  Price = 20m }
        );
        await ctx.SaveChangesAsync();

        var names = await ctx.Products.SelectToListAsync(p => p.Name);
        Assert.Equal(2, names.Count);
        Assert.Contains("Alpha", names);
        Assert.Contains("Beta", names);
    }

    [Fact]
    public async Task SelectFirstOrDefaultAsync_ReturnsFirstProjected()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.Add(new Product { Name = "Only", Price = 5m });
        await ctx.SaveChangesAsync();

        var name = await ctx.Products.SelectFirstOrDefaultAsync(p => p.Name);
        Assert.Equal("Only", name);
    }

    [Fact]
    public async Task SelectToPagedAsync_PaginatesProjection()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(Enumerable.Range(1, 5)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await ctx.SaveChangesAsync();

        var page = await ctx.Products
            .OrderBy(p => p.Id)
            .SelectToPagedAsync(p => p.Name, page: 1, pageSize: 3);

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.Items.Count);
        Assert.All(page.Items, item => Assert.IsType<string>(item));
    }

    [Fact]
    public async Task SelectDistinctAsync_ReturnsDistinct()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Widget", Price = 10m },
            new Product { Name = "Widget", Price = 20m },
            new Product { Name = "Gadget", Price = 30m }
        );
        await ctx.SaveChangesAsync();

        var distinctNames = await ctx.Products.SelectDistinctAsync(p => p.Name);
        Assert.Equal(2, distinctNames.Count);
        Assert.Contains("Widget", distinctNames);
        Assert.Contains("Gadget", distinctNames);
    }

    // ── WithNoTracking ────────────────────────────────────────────────

    [Fact]
    public async Task WithNoTracking_ReturnsUntrackedEntities()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.Add(new Product { Name = "Untracked", Price = 1m });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var results = await ctx.Products.WithNoTracking().ToListAsync();

        Assert.Single(results);
        Assert.Empty(ctx.ChangeTracker.Entries());
    }

    // ── IncludeDeleted / OnlyDeleted ──────────────────────────────────

    [Fact]
    public async Task IncludeDeleted_ReturnsAllIncludingDeleted()
    {
        await using var ctx = DbFactory.CreateWithSoftDelete();
        var order = new Order { Title = "Test", Total = 1m, CustomerId = 1 };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();
        ctx.Orders.Remove(order);
        await ctx.SaveChangesAsync();

        var all = await ctx.Orders.IncludeDeleted().ToListAsync();
        Assert.Single(all);
        Assert.True(all[0].IsDeleted);
    }

    [Fact]
    public async Task OnlyDeleted_ReturnsOnlySoftDeleted()
    {
        await using var ctx = DbFactory.CreateWithSoftDelete();
        var active  = new Order { Title = "Active",  Total = 1m, CustomerId = 1 };
        var deleted = new Order { Title = "Deleted", Total = 2m, CustomerId = 1 };
        ctx.Orders.AddRange(active, deleted);
        await ctx.SaveChangesAsync();
        ctx.Orders.Remove(deleted);
        await ctx.SaveChangesAsync();

        var deletedOnly = await ctx.Orders.OnlyDeleted().ToListAsync();
        Assert.Single(deletedOnly);
        Assert.Equal("Deleted", deletedOnly[0].Title);
    }
}
