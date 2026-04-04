namespace EfCoreKit.Tests.Integration;

public class AuditTrailTests
{
    [Fact]
    public async Task Add_SetsCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        await using var ctx = DbFactory.CreateWithAudit();

        ctx.AuditedItems.Add(new AuditedItem { Name = "Item A" });
        await ctx.SaveChangesAsync();

        var item = ctx.AuditedItems.First();
        Assert.True(item.CreatedAt >= before);
        Assert.True(item.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Add_SetsCreatedBy_WhenUserProvided()
    {
        await using var ctx = DbFactory.CreateWithAudit(new StaticUserProvider("alice"));

        ctx.AuditedItems.Add(new AuditedItem { Name = "Item B" });
        await ctx.SaveChangesAsync();

        Assert.Equal("alice", ctx.AuditedItems.First().CreatedBy);
    }

    [Fact]
    public async Task Update_SetsUpdatedAt_AndPreservesCreatedAt()
    {
        await using var ctx = DbFactory.CreateWithAudit();

        ctx.AuditedItems.Add(new AuditedItem { Name = "Before" });
        await ctx.SaveChangesAsync();

        var item = ctx.AuditedItems.First();
        var originalCreatedAt = item.CreatedAt;

        await Task.Delay(10);

        item.Name = "After";
        ctx.AuditedItems.Update(item);
        await ctx.SaveChangesAsync();

        var updated = ctx.AuditedItems.First();
        Assert.Equal(originalCreatedAt, updated.CreatedAt);  // CreatedAt must not change
        Assert.NotNull(updated.UpdatedAt);
        Assert.True(updated.UpdatedAt >= originalCreatedAt);
    }

    [Fact]
    public async Task Update_SetsUpdatedBy_WhenUserProvided()
    {
        await using var ctx = DbFactory.CreateWithAudit(new StaticUserProvider("bob"));

        ctx.AuditedItems.Add(new AuditedItem { Name = "Original" });
        await ctx.SaveChangesAsync();

        var item = ctx.AuditedItems.First();
        item.Name = "Updated";
        ctx.AuditedItems.Update(item);
        await ctx.SaveChangesAsync();

        Assert.Equal("bob", ctx.AuditedItems.First().UpdatedBy);
    }
}
