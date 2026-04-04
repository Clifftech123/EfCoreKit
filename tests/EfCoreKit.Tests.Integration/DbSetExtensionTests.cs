using EfCoreKit.Extensions;

namespace EfCoreKit.Tests.Integration;

public class DbSetExtensionTests
{
    // ── Aggregates ────────────────────────────────────────────────────

    [Fact]
    public async Task MaxAsync_ReturnsMaximumValue()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 10m },
            new Product { Name = "B", Price = 50m },
            new Product { Name = "C", Price = 30m }
        );
        await ctx.SaveChangesAsync();

        var max = await ctx.Products.MaxAsync(p => p.Price);
        Assert.Equal(50m, max);
    }

    [Fact]
    public async Task MinAsync_ReturnsMinimumValue()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 10m },
            new Product { Name = "B", Price = 50m },
            new Product { Name = "C", Price = 30m }
        );
        await ctx.SaveChangesAsync();

        var min = await ctx.Products.MinAsync(p => p.Price);
        Assert.Equal(10m, min);
    }

    [Fact]
    public async Task SumAsync_ReturnsSum()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 10m },
            new Product { Name = "B", Price = 20m },
            new Product { Name = "C", Price = 30m }
        );
        await ctx.SaveChangesAsync();

        var sum = await ctx.Products.SumAsync(p => p.Price);
        Assert.Equal(60m, sum);
    }

    [Fact]
    public async Task AverageAsync_Decimal_ReturnsAverage()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 10m },
            new Product { Name = "B", Price = 20m },
            new Product { Name = "C", Price = 30m }
        );
        await ctx.SaveChangesAsync();

        var avg = await ctx.Products.AverageAsync(p => p.Price);
        Assert.Equal(20m, avg);
    }

    // ── AnyAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AnyAsync_ReturnsTrue_WhenEntitiesExist()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.Add(new Product { Name = "Exists", Price = 1m });
        await ctx.SaveChangesAsync();

        Assert.True(await ctx.Products.AnyAsync());
    }

    [Fact]
    public async Task AnyAsync_ReturnsFalse_WhenEmpty()
    {
        await using var ctx = DbFactory.CreateBasic();
        Assert.False(await ctx.Products.AnyAsync());
    }

    // ── LongCountAsync ────────────────────────────────────────────────

    [Fact]
    public async Task LongCountAsync_ReturnsCount()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 1m },
            new Product { Name = "B", Price = 2m }
        );
        await ctx.SaveChangesAsync();

        Assert.Equal(2L, await ctx.Products.LongCountAsync());
    }

    [Fact]
    public async Task LongCountAsync_WithPredicate_CountsFiltered()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Active",   Price = 1m, IsActive = true },
            new Product { Name = "Inactive", Price = 2m, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        Assert.Equal(1L, await ctx.Products.LongCountAsync(p => p.IsActive));
    }

    // ── GetByIdsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdsAsync_ReturnsMatchingEntities()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 1m },
            new Product { Name = "B", Price = 2m },
            new Product { Name = "C", Price = 3m }
        );
        await ctx.SaveChangesAsync();

        var allProducts = await ctx.Products.ToListAsync();
        var idsToFind = allProducts.Where(p => p.Name is "A" or "C").Select(p => p.Id).ToList();

        var result = await ctx.Products.GetByIdsAsync(p => p.Id, idsToFind);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "A");
        Assert.Contains(result, p => p.Name == "C");
    }

    // ── ExistsAsync with predicate ────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_Predicate_ReturnsTrueWhenMatch()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.Add(new Product { Name = "Active", Price = 1m, IsActive = true });
        await ctx.SaveChangesAsync();

        Assert.True(await ctx.Products.ExistsAsync(p => p.IsActive));
        Assert.False(await ctx.Products.ExistsAsync(p => p.Name == "NonExistent"));
    }

    // ── CountAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CountAsync_NoArgs_ReturnsAll()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 1m },
            new Product { Name = "B", Price = 2m }
        );
        await ctx.SaveChangesAsync();

        Assert.Equal(2, await ctx.Products.CountAsync());
    }

    // ── Lookups ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var ctx = DbFactory.CreateBasic();
        var result = await ctx.Products.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirst_WhenMultipleMatch()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Match", Price = 1m, IsActive = true },
            new Product { Name = "Match", Price = 2m, IsActive = true }
        );
        await ctx.SaveChangesAsync();

        var result = await ctx.Products.FirstOrDefaultAsync(p => p.IsActive);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsNull_WhenNoMatch()
    {
        await using var ctx = DbFactory.CreateBasic();
        var result = await ctx.Products.SingleOrDefaultAsync(p => p.Name == "Ghost");
        Assert.Null(result);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAll()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 1m },
            new Product { Name = "B", Price = 2m }
        );
        await ctx.SaveChangesAsync();

        var all = await ctx.Products.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    // ── FindAsync (predicate) ─────────────────────────────────────────

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Match", Price = 1m, IsActive = true },
            new Product { Name = "NoMatch", Price = 2m, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        var result = await ctx.Products.FindAsync(p => p.IsActive);
        Assert.Single(result);
        Assert.Equal("Match", result[0].Name);
    }

    // ── RemoveRangeAsync ──────────────────────────────────────────────

    [Fact]
    public async Task RemoveRangeAsync_RemovesMatchingEntities()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(
            new Product { Name = "Keep",   Price = 1m, IsActive = true },
            new Product { Name = "Remove", Price = 2m, IsActive = false }
        );
        await ctx.SaveChangesAsync();

        await ctx.Products.RemoveRangeAsync(p => !p.IsActive);
        await ctx.SaveChangesAsync();

        var remaining = await ctx.Products.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("Keep", remaining[0].Name);
    }
}
