using EfCoreKit.Entities;

namespace EfCoreKit.Tests.Integration;

public class FullAuditLogTests
{
    [Fact]
    public async Task Add_WritesAuditLogForAllProperties()
    {
        var user = new StaticUserProvider("audit-user");
        await using var ctx = DbFactory.CreateWithFullAudit(user);

        ctx.FullAuditedItems.Add(new FullAuditedItem { Name = "TestItem", Value = 42m });
        await ctx.SaveChangesAsync();

        var logs = await ctx.AuditLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.All(logs, l =>
        {
            Assert.Equal("Added", l.Action);
            Assert.Equal("FullAuditedItem", l.EntityType);
            Assert.Null(l.OldValue);
            Assert.Equal("audit-user", l.ChangedBy);
        });

        // Should have a log for the Name property
        var nameLog = logs.FirstOrDefault(l => l.PropertyName == "Name");
        Assert.NotNull(nameLog);
        Assert.Equal("TestItem", nameLog!.NewValue);
    }

    [Fact]
    public async Task Modify_WritesAuditLogOnlyForChangedProperties()
    {
        var user = new StaticUserProvider("editor");
        await using var ctx = DbFactory.CreateWithFullAudit(user);

        ctx.FullAuditedItems.Add(new FullAuditedItem { Name = "Original", Value = 10m });
        await ctx.SaveChangesAsync();

        // Clear existing add logs
        ctx.AuditLogs.RemoveRange(ctx.AuditLogs);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var item = await ctx.FullAuditedItems.FirstAsync();
        item.Name = "Updated";
        ctx.FullAuditedItems.Update(item);
        await ctx.SaveChangesAsync();

        var modLogs = await ctx.AuditLogs.Where(l => l.Action == "Modified").ToListAsync();
        Assert.NotEmpty(modLogs);

        var nameLog = modLogs.FirstOrDefault(l => l.PropertyName == "Name");
        Assert.NotNull(nameLog);
        Assert.Equal("Original", nameLog!.OldValue);
        Assert.Equal("Updated", nameLog.NewValue);
    }

    [Fact]
    public async Task AuditLog_EntityKey_IsPopulated()
    {
        await using var ctx = DbFactory.CreateWithFullAudit();

        ctx.FullAuditedItems.Add(new FullAuditedItem { Name = "KeyTest", Value = 1m });
        await ctx.SaveChangesAsync();

        var log = await ctx.AuditLogs.FirstAsync();
        Assert.False(string.IsNullOrEmpty(log.EntityKey));
    }

    [Fact]
    public async Task AuditLog_ChangedAt_IsSetToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        await using var ctx = DbFactory.CreateWithFullAudit();

        ctx.FullAuditedItems.Add(new FullAuditedItem { Name = "TimeTest", Value = 5m });
        await ctx.SaveChangesAsync();

        var log = await ctx.AuditLogs.FirstAsync();
        Assert.True(log.ChangedAt >= before);
        Assert.True(log.ChangedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task BasicAudit_DoesNotWriteAuditLogs_WhenFullLogDisabled()
    {
        // AuditDbContext has EnableAuditTrail() with fullLog: false
        await using var ctx = DbFactory.CreateWithAudit();

        ctx.AuditedItems.Add(new AuditedItem { Name = "NoLog" });
        await ctx.SaveChangesAsync();

        var logs = await ctx.AuditLogs.ToListAsync();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task Add_WithNoUserProvider_ChangedByIsNull()
    {
        await using var ctx = DbFactory.CreateWithFullAudit(); // no user

        ctx.FullAuditedItems.Add(new FullAuditedItem { Name = "NoUser", Value = 0m });
        await ctx.SaveChangesAsync();

        var log = await ctx.AuditLogs.FirstAsync();
        Assert.Null(log.ChangedBy);
    }
}
