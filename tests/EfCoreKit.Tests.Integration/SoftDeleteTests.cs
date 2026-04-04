using EfCoreKit.Extensions;

namespace EfCoreKit.Tests.Integration;

public class SoftDeleteTests
{
    [Fact]
    public async Task Remove_ConvertsToBSoftDelete_NotHardDelete()
    {
        await using var ctx = DbFactory.CreateWithSoftDelete();

        var order = new Order { Title = "Order #1", Total = 99m, CustomerId = 1 };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.Orders.Remove(order);
        await ctx.SaveChangesAsync();

        var raw = await ctx.Orders.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == order.Id);
        Assert.NotNull(raw);
        Assert.True(raw!.IsDeleted);
        Assert.NotNull(raw.DeletedAt);
    }

    [Fact]
    public async Task SoftDeleted_Entity_IsHiddenByDefaultFilter()
    {
        await using var ctx = DbFactory.CreateWithSoftDelete();

        var order = new Order { Title = "Invisible", Total = 1m, CustomerId = 2 };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.Orders.Remove(order);
        await ctx.SaveChangesAsync();

        var visible = await ctx.Orders.ToListAsync();
        Assert.DoesNotContain(visible, o => o.Id == order.Id);
    }

    [Fact]
    public async Task GetDeletedAsync_ReturnsOnlySoftDeletedEntities()
    {
        await using var ctx = DbFactory.CreateWithSoftDelete();

        var active  = new Order { Title = "Active",  Total = 10m, CustomerId = 1 };
        var deleted = new Order { Title = "Deleted", Total = 20m, CustomerId = 1 };
        ctx.Orders.AddRange(active, deleted);
        await ctx.SaveChangesAsync();

        ctx.Orders.Remove(deleted);
        await ctx.SaveChangesAsync();

        var deletedList = await ctx.Orders.GetDeletedAsync();
        Assert.Single(deletedList);
        Assert.Equal("Deleted", deletedList[0].Title);
    }

    [Fact]
    public async Task Restore_ClearsSoftDeleteFields()
    {
        await using var ctx = DbFactory.CreateWithSoftDelete();

        var order = new Order { Title = "Restore Me", Total = 5m, CustomerId = 1 };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.Orders.Remove(order);
        await ctx.SaveChangesAsync();

        var deleted = (await ctx.Orders.GetDeletedAsync()).First(o => o.Id == order.Id);
        ctx.Orders.Restore(deleted);
        await ctx.SaveChangesAsync();

        var restored = await ctx.Orders.IgnoreQueryFilters().FirstAsync(o => o.Id == order.Id);
        Assert.False(restored.IsDeleted);
        Assert.Null(restored.DeletedAt);
        Assert.Null(restored.DeletedBy);
    }

    [Fact]
    public async Task HardDelete_PermanentlyRemovesEntity()
    {
        // BasicDbContext has no soft-delete interceptor, so Remove() physically deletes.
        await using var ctx = DbFactory.CreateBasic();

        var order = new Order { Title = "Hard Delete", Total = 77m, CustomerId = 3 };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.Orders.HardDelete(order);
        await ctx.SaveChangesAsync();

        var raw = await ctx.Orders.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == order.Id);
        Assert.Null(raw);
    }

    [Fact]
    public async Task SoftDelete_SetsDeletedBy_WhenUserProviderConfigured()
    {
        var user = new StaticUserProvider("user-42");
        await using var ctx = DbFactory.CreateWithSoftDelete(user);

        var order = new Order { Title = "Attributed Delete", Total = 50m, CustomerId = 1 };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.Orders.Remove(order);
        await ctx.SaveChangesAsync();

        var raw = await ctx.Orders.IgnoreQueryFilters().FirstAsync(o => o.Id == order.Id);
        Assert.Equal("user-42", raw.DeletedBy);
    }
}
