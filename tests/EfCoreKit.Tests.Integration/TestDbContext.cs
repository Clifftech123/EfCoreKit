using EfCoreKit.Context;
using EfCoreKit.Entities;
using EfCoreKit.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Tests.Integration;

// ── Entities ─────────────────────────────────────────────────────────────────

public class Product : BaseEntity
{
    public string Name  { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Order : SoftDeletableEntity
{
    public string Title      { get; set; } = string.Empty;
    public decimal Total     { get; set; }
    public int CustomerId    { get; set; }
}

public class AuditedItem : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
}

public class TenantNote : FullEntity
{
    public string Content { get; set; } = string.Empty;
}

// ── DbContext types (one per feature combination) ─────────────────────────────
//
// EF Core caches the compiled model once per DbContext *type*. Using a separate
// type for each feature combination ensures each gets its own model with the
// correct global query filters applied.

/// <summary>Plain context — no features enabled.</summary>
public class BasicDbContext : EfCoreDbContext<BasicDbContext>
{
    public DbSet<Product>  Products  => Set<Product>();
    public DbSet<Order>    Orders    => Set<Order>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public BasicDbContext(DbContextOptions<BasicDbContext> options, EfCoreOptions kitOptions)
        : base(options, kitOptions) { }
}

/// <summary>Context with soft-delete enabled.</summary>
public class SoftDeleteDbContext : EfCoreDbContext<SoftDeleteDbContext>
{
    public DbSet<Order>    Orders    => Set<Order>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public SoftDeleteDbContext(
        DbContextOptions<SoftDeleteDbContext> options,
        EfCoreOptions kitOptions,
        IUserProvider? user = null)
        : base(options, kitOptions, user, null) { }
}

/// <summary>Context with audit trail enabled.</summary>
public class AuditDbContext : EfCoreDbContext<AuditDbContext>
{
    public DbSet<AuditedItem> AuditedItems => Set<AuditedItem>();
    public DbSet<AuditLog>    AuditLogs    => Set<AuditLog>();

    public AuditDbContext(
        DbContextOptions<AuditDbContext> options,
        EfCoreOptions kitOptions,
        IUserProvider? user = null)
        : base(options, kitOptions, user, null) { }
}

/// <summary>Context with soft-delete + multi-tenancy enabled.</summary>
public class TenantDbContext : EfCoreDbContext<TenantDbContext>
{
    public DbSet<TenantNote> TenantNotes => Set<TenantNote>();
    public DbSet<AuditLog>   AuditLogs   => Set<AuditLog>();

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        EfCoreOptions kitOptions,
        ITenantProvider? tenant = null)
        : base(options, kitOptions, null, tenant) { }
}

// ── Factory ───────────────────────────────────────────────────────────────────

public static class DbFactory
{
    public static BasicDbContext CreateBasic()
    {
        var opts = new DbContextOptionsBuilder<BasicDbContext>()
            .UseSqlite("DataSource=:memory:").Options;
        var ctx = new BasicDbContext(opts, new EfCoreOptions());
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public static SoftDeleteDbContext CreateWithSoftDelete(IUserProvider? user = null)
    {
        var opts = new DbContextOptionsBuilder<SoftDeleteDbContext>()
            .UseSqlite("DataSource=:memory:").Options;
        var ctx = new SoftDeleteDbContext(opts, new EfCoreOptions().EnableSoftDelete(), user);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public static AuditDbContext CreateWithAudit(IUserProvider? user = null)
    {
        var opts = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlite("DataSource=:memory:").Options;
        var ctx = new AuditDbContext(opts, new EfCoreOptions().EnableAuditTrail(), user);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    public static TenantDbContext CreateWithTenancy(ITenantProvider provider)
    {
        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite("DataSource=:memory:").Options;
        var ctx = new TenantDbContext(opts, new EfCoreOptions().EnableSoftDelete().EnableMultiTenancy(), provider);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        return ctx;
    }
}

// ── Test stubs ────────────────────────────────────────────────────────────────

public class StaticUserProvider : IUserProvider
{
    private readonly string _userId;
    public StaticUserProvider(string userId) => _userId = userId;
    public string? GetCurrentUserId()   => _userId;
    public string? GetCurrentUserName() => _userId;
}

public class StaticTenantProvider : ITenantProvider
{
    private readonly string _tenantId;
    public StaticTenantProvider(string tenantId) => _tenantId = tenantId;
    public string? GetCurrentTenantId() => _tenantId;
}

/// <summary>
/// Tenant provider whose current ID can be changed at runtime — mirrors a
/// production singleton provider backed by HttpContext.
/// </summary>
public class MutableTenantProvider : ITenantProvider
{
    public string? CurrentTenantId { get; set; }
    public string? GetCurrentTenantId() => CurrentTenantId;
}
