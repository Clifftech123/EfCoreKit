using EfCoreKit.Exceptions;

namespace EfCoreKit.Tests.Integration;

public class TenantValidationTests
{
    private static readonly MutableTenantProvider _provider = new();

    [Fact]
    public async Task TenantId_IsNotOverwritten_WhenAlreadySetOnAdd()
    {
        _provider.CurrentTenantId = "tenant-default";
        await using var ctx = DbFactory.CreateWithTenancy(_provider);

        var note = new TenantNote { Content = "Pre-assigned", TenantId = "tenant-explicit" };
        ctx.TenantNotes.Add(note);
        await ctx.SaveChangesAsync();

        var saved = await ctx.TenantNotes.IgnoreQueryFilters().FirstAsync();
        Assert.Equal("tenant-explicit", saved.TenantId);
    }

    [Fact]
    public async Task Modify_ThrowsTenantMismatchException_WhenTenantChanges()
    {
        _provider.CurrentTenantId = "tenant-A";
        await using var ctx = DbFactory.CreateWithTenancy(_provider);

        ctx.TenantNotes.Add(new TenantNote { Content = "A's note" });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        // Switch to tenant B and try to modify tenant A's entity
        _provider.CurrentTenantId = "tenant-B";
        var note = await ctx.TenantNotes.IgnoreQueryFilters().FirstAsync();
        note.Content = "Modified by B";
        ctx.TenantNotes.Update(note);

        await Assert.ThrowsAsync<TenantMismatchException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task TenantId_CannotBeMutated_OnUpdate()
    {
        _provider.CurrentTenantId = "tenant-X";
        await using var ctx = DbFactory.CreateWithTenancy(_provider);

        ctx.TenantNotes.Add(new TenantNote { Content = "Original" });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var note = await ctx.TenantNotes.IgnoreQueryFilters().FirstAsync();
        note.TenantId = "tenant-X-hacked"; // try to mutate
        note.Content = "Updated";
        ctx.TenantNotes.Update(note);
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();
        var saved = await ctx.TenantNotes.IgnoreQueryFilters().FirstAsync();
        Assert.Equal("tenant-X", saved.TenantId); // mutation prevented
    }

    [Fact]
    public async Task TenantId_AssignedFromProvider_WhenNullOnAdd()
    {
        _provider.CurrentTenantId = "auto-tenant";
        await using var ctx = DbFactory.CreateWithTenancy(_provider);

        ctx.TenantNotes.Add(new TenantNote { Content = "Auto-assigned", TenantId = null });
        await ctx.SaveChangesAsync();

        var saved = await ctx.TenantNotes.IgnoreQueryFilters().FirstAsync();
        Assert.Equal("auto-tenant", saved.TenantId);
    }

    [Fact]
    public async Task SoftDeletedTenantEntity_StillFilteredByTenant()
    {
        _provider.CurrentTenantId = "tenant-filter";
        await using var ctx = DbFactory.CreateWithTenancy(_provider);

        var note = new TenantNote { Content = "Will be deleted" };
        ctx.TenantNotes.Add(note);
        await ctx.SaveChangesAsync();

        ctx.TenantNotes.Remove(note);
        await ctx.SaveChangesAsync();

        // Default query (soft-delete + tenant filter) should return nothing
        var visible = await ctx.TenantNotes.ToListAsync();
        Assert.Empty(visible);

        // IgnoreQueryFilters bypasses both filters
        var all = await ctx.TenantNotes.IgnoreQueryFilters().ToListAsync();
        Assert.Single(all);
        Assert.True(all[0].IsDeleted);
    }
}
