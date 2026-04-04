using EfCoreKit.Extensions;
using EfCoreKit.Models;

namespace EfCoreKit.Tests.Integration;

public class KeysetPaginationTests
{
    // For int keys, cursor=0 acts as "first page" since SQLite auto-increment starts at 1
    // and the filter is WHERE Id > 0 which matches all rows.

    [Fact]
    public async Task FirstPage_ReturnsItemsAndHasMore()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(Enumerable.Range(1, 5)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await ctx.SaveChangesAsync();

        var result = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: 0, pageSize: 3);

        Assert.Equal(3, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task SecondPage_UsesCursorAndReturnsDifferentItems()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(Enumerable.Range(1, 5)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await ctx.SaveChangesAsync();

        var page1 = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: 0, pageSize: 3);

        var cursor = int.Parse(page1.NextCursor!);
        var page2 = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: cursor, pageSize: 3);

        Assert.Equal(2, page2.Items.Count);
        Assert.False(page2.HasMore);
        Assert.Null(page2.NextCursor);
        Assert.NotNull(page2.PreviousCursor);

        // No overlap between pages
        var page1Ids = page1.Items.Select(p => p.Id).ToHashSet();
        Assert.DoesNotContain(page2.Items, p => page1Ids.Contains(p.Id));
    }

    [Fact]
    public async Task EmptyResult_ReturnsNoItemsAndNoMore()
    {
        await using var ctx = DbFactory.CreateBasic();
        // No products added

        var result = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: 0, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task ExactPageSize_HasMoreIsFalse()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(Enumerable.Range(1, 3)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await ctx.SaveChangesAsync();

        var result = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: 0, pageSize: 3);

        Assert.Equal(3, result.Items.Count);
        Assert.False(result.HasMore);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task PreviousCursor_IsSetOnSubsequentPages()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(Enumerable.Range(1, 10)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await ctx.SaveChangesAsync();

        var page1 = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: 0, pageSize: 3);

        var cursor = int.Parse(page1.NextCursor!);
        var page2 = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: cursor, pageSize: 3);

        Assert.Equal(cursor.ToString(), page2.PreviousCursor);
    }

    [Fact]
    public async Task KeysetPagedResult_ContainsCorrectMetadata()
    {
        await using var ctx = DbFactory.CreateBasic();
        ctx.Products.AddRange(Enumerable.Range(1, 5)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await ctx.SaveChangesAsync();

        var page1 = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: 0, pageSize: 2);

        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.HasMore);

        var id = int.Parse(page1.NextCursor!);
        var page2 = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: id, pageSize: 2);

        Assert.Equal(2, page2.Items.Count);
        Assert.True(page2.HasMore);

        var id2 = int.Parse(page2.NextCursor!);
        var page3 = await ctx.Products
            .OrderBy(p => p.Id)
            .ToKeysetPagedAsync(p => p.Id, cursor: id2, pageSize: 2);

        Assert.Single(page3.Items);
        Assert.False(page3.HasMore);
        Assert.Null(page3.NextCursor);
    }
}
