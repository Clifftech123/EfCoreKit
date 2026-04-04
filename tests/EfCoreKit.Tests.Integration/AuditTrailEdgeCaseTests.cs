namespace EfCoreKit.Tests.Integration;

public class AuditTrailEdgeCaseTests
{
    [Fact]
    public async Task Add_WithNoUserProvider_CreatedByIsNull()
    {
        await using var ctx = DbFactory.CreateWithAudit(); // no user provider

        ctx.AuditedItems.Add(new AuditedItem { Name = "NoUser" });
        await ctx.SaveChangesAsync();

        var item = ctx.AuditedItems.First();
        Assert.Null(item.CreatedBy);
        Assert.NotEqual(default, item.CreatedAt);
    }

    [Fact]
    public async Task Update_DoesNotOverwriteCreatedBy()
    {
        await using var ctx = DbFactory.CreateWithAudit(new StaticUserProvider("original-author"));

        ctx.AuditedItems.Add(new AuditedItem { Name = "Created" });
        await ctx.SaveChangesAsync();

        var item = ctx.AuditedItems.First();
        Assert.Equal("original-author", item.CreatedBy);

        // Now update — CreatedBy should NOT change
        item.Name = "Modified";
        ctx.AuditedItems.Update(item);
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();
        var updated = await ctx.AuditedItems.FirstAsync();
        Assert.Equal("original-author", updated.CreatedBy);
        Assert.Equal("original-author", updated.UpdatedBy);
    }

    [Fact]
    public async Task MultipleAdds_EachGetOwnCreatedAt()
    {
        await using var ctx = DbFactory.CreateWithAudit();

        ctx.AuditedItems.AddRange(
            new AuditedItem { Name = "First" },
            new AuditedItem { Name = "Second" }
        );
        await ctx.SaveChangesAsync();

        var items = await ctx.AuditedItems.ToListAsync();
        Assert.Equal(2, items.Count);
        // Both should have CreatedAt set (may be same timestamp since same SaveChanges call)
        Assert.All(items, i => Assert.NotEqual(default, i.CreatedAt));
    }

    [Fact]
    public async Task Update_SetsUpdatedAt_EvenWhenNoUserProvider()
    {
        await using var ctx = DbFactory.CreateWithAudit(); // no user

        ctx.AuditedItems.Add(new AuditedItem { Name = "Before" });
        await ctx.SaveChangesAsync();

        var item = ctx.AuditedItems.First();
        item.Name = "After";
        ctx.AuditedItems.Update(item);
        await ctx.SaveChangesAsync();

        Assert.NotNull(ctx.AuditedItems.First().UpdatedAt);
        Assert.Null(ctx.AuditedItems.First().UpdatedBy);
    }
}
