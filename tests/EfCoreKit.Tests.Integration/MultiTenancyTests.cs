namespace EfCoreKit.Tests.Integration;

public class MultiTenancyTests
{
    // EF Core caches the compiled model per DbContext type. The tenant query filter
    // captures the provider INSTANCE at model-build time. Using one shared static
    // instance means all tests in this class use the same captured provider, and
    // switching CurrentTenantId changes what queries return — just like a production
    // singleton provider backed by HttpContext.
    private static readonly MutableTenantProvider _provider = new();

    [Fact]
    public async Task TenantInterceptor_AssignsTenantId_OnAdd()
    {
        _provider.CurrentTenantId = "tenant-1";
        await using var ctx = DbFactory.CreateWithTenancy(_provider);

        ctx.TenantNotes.Add(new TenantNote { Content = "Hello" });
        await ctx.SaveChangesAsync();

        var note = await ctx.TenantNotes.IgnoreQueryFilters().FirstAsync();
        Assert.Equal("tenant-1", note.TenantId);
    }

    [Fact]
    public async Task QueryFilter_IsolatesEachTenant()
    {
        await using var ctx = DbFactory.CreateWithTenancy(_provider);

        _provider.CurrentTenantId = "A";
        ctx.TenantNotes.Add(new TenantNote { Content = "A note" });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        _provider.CurrentTenantId = "B";
        ctx.TenantNotes.Add(new TenantNote { Content = "B note" });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        _provider.CurrentTenantId = "A";
        var aResults = await ctx.TenantNotes.ToListAsync();
        Assert.Single(aResults);
        Assert.Equal("A note", aResults[0].Content);

        _provider.CurrentTenantId = "B";
        var bResults = await ctx.TenantNotes.ToListAsync();
        Assert.Single(bResults);
        Assert.Equal("B note", bResults[0].Content);
    }

    [Fact]
    public async Task IgnoreQueryFilters_ReturnsAllTenants()
    {
        await using var ctx = DbFactory.CreateWithTenancy(_provider);

        _provider.CurrentTenantId = "X";
        ctx.TenantNotes.Add(new TenantNote { Content = "X note" });
        await ctx.SaveChangesAsync();

        _provider.CurrentTenantId = "Y";
        ctx.TenantNotes.Add(new TenantNote { Content = "Y note" });
        await ctx.SaveChangesAsync();

        var all = await ctx.TenantNotes.IgnoreQueryFilters().ToListAsync();
        Assert.Equal(2, all.Count);
    }
}
