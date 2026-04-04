using EfCoreKit.Extensions;

namespace EfCoreKit.Tests.Integration;

public class SoftDeleteCascadeTests
{
    [Fact]
    public async Task CascadeDelete_SoftDeletesLoadedChildren()
    {
        await using var ctx = DbFactory.CreateWithCascadeSoftDelete();

        var invoice = new Invoice
        {
            Number = "INV-001",
            Lines =
            [
                new InvoiceLine { Description = "Line A", Amount = 10m },
                new InvoiceLine { Description = "Line B", Amount = 20m }
            ]
        };
        ctx.Invoices.Add(invoice);
        await ctx.SaveChangesAsync();

        // Load children into the change tracker
        await ctx.InvoiceLines.Where(l => l.InvoiceId == invoice.Id).LoadAsync();

        ctx.Invoices.Remove(invoice);
        await ctx.SaveChangesAsync();

        var lines = await ctx.InvoiceLines.IgnoreQueryFilters().ToListAsync();
        Assert.All(lines, l =>
        {
            Assert.True(l.IsDeleted);
            Assert.NotNull(l.DeletedAt);
        });
    }

    [Fact]
    public async Task CascadeDelete_SkipsAlreadyDeletedChildren()
    {
        var user = new StaticUserProvider("cascade-user");
        await using var ctx = DbFactory.CreateWithCascadeSoftDelete(user);

        var invoice = new Invoice
        {
            Number = "INV-002",
            Lines =
            [
                new InvoiceLine { Description = "Active", Amount = 10m },
                new InvoiceLine { Description = "AlreadyDeleted", Amount = 20m }
            ]
        };
        ctx.Invoices.Add(invoice);
        await ctx.SaveChangesAsync();

        // Manually soft-delete one child first
        var lineToDelete = await ctx.InvoiceLines.FirstAsync(l => l.Description == "AlreadyDeleted");
        ctx.InvoiceLines.Remove(lineToDelete);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        // Reload all children and cascade delete the parent
        var parentInvoice = await ctx.Invoices.IgnoreQueryFilters().FirstAsync(i => i.Id == invoice.Id);
        await ctx.InvoiceLines.IgnoreQueryFilters()
            .Where(l => l.InvoiceId == parentInvoice.Id).LoadAsync();

        ctx.Invoices.Remove(parentInvoice);
        await ctx.SaveChangesAsync();

        var allLines = await ctx.InvoiceLines.IgnoreQueryFilters().ToListAsync();
        Assert.All(allLines, l => Assert.True(l.IsDeleted));
    }

    [Fact]
    public async Task CascadeDelete_SetsDeletedByOnChildren()
    {
        var user = new StaticUserProvider("admin");
        await using var ctx = DbFactory.CreateWithCascadeSoftDelete(user);

        var invoice = new Invoice
        {
            Number = "INV-003",
            Lines = [new InvoiceLine { Description = "Child", Amount = 5m }]
        };
        ctx.Invoices.Add(invoice);
        await ctx.SaveChangesAsync();

        await ctx.InvoiceLines.Where(l => l.InvoiceId == invoice.Id).LoadAsync();

        ctx.Invoices.Remove(invoice);
        await ctx.SaveChangesAsync();

        var line = await ctx.InvoiceLines.IgnoreQueryFilters().FirstAsync();
        Assert.Equal("admin", line.DeletedBy);
    }

    [Fact]
    public async Task CascadeDelete_ParentIsAlsoSoftDeleted()
    {
        await using var ctx = DbFactory.CreateWithCascadeSoftDelete();

        var invoice = new Invoice
        {
            Number = "INV-004",
            Lines = [new InvoiceLine { Description = "Only child", Amount = 1m }]
        };
        ctx.Invoices.Add(invoice);
        await ctx.SaveChangesAsync();

        await ctx.InvoiceLines.Where(l => l.InvoiceId == invoice.Id).LoadAsync();

        ctx.Invoices.Remove(invoice);
        await ctx.SaveChangesAsync();

        var parent = await ctx.Invoices.IgnoreQueryFilters().FirstAsync(i => i.Id == invoice.Id);
        Assert.True(parent.IsDeleted);
        Assert.NotNull(parent.DeletedAt);
    }

    [Fact]
    public async Task NonCascadeContext_DoesNotSoftDeleteChildren()
    {
        // SoftDeleteDbContext does NOT have cascade enabled
        await using var ctx = DbFactory.CreateWithCascadeSoftDelete();

        var invoice = new Invoice
        {
            Number = "INV-005",
            Lines = [new InvoiceLine { Description = "Orphan", Amount = 15m }]
        };
        ctx.Invoices.Add(invoice);
        await ctx.SaveChangesAsync();

        // Do NOT load children into the tracker — cascade can't touch what's not loaded
        ctx.Invoices.Remove(invoice);
        await ctx.SaveChangesAsync();

        var lines = await ctx.InvoiceLines.IgnoreQueryFilters().ToListAsync();
        Assert.Single(lines);
        Assert.False(lines[0].IsDeleted);
    }
}
