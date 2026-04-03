# Getting Started

## Installation

Install the umbrella package (includes all database providers):

```bash
dotnet add package EfCoreKit
```

Or install only what you need:

```bash
dotnet add package EfCoreKit.Core          # Core features (no bulk ops)
dotnet add package EfCoreKit.SqlServer     # + SQL Server bulk operations
dotnet add package EfCoreKit.PostgreSql    # + PostgreSQL bulk operations
dotnet add package EfCoreKit.MySql         # + MySQL bulk operations
dotnet add package EfCoreKit.Sqlite        # + SQLite bulk operations
```

## 1. Create Your DbContext

Inherit from `EfCoreKitDbContext<T>` instead of `DbContext`:

```csharp
using EfCoreKit.Core.Context;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : EfCoreKitDbContext<AppDbContext>
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        EfCoreKitOptions kitOptions,
        IUserProvider? userProvider = null,
        ITenantProvider? tenantProvider = null)
        : base(options, kitOptions, userProvider, tenantProvider) { }
}
```

> **Tip:** You don't _have_ to inherit from `EfCoreKitDbContext<T>`. The extension methods and interceptors work with any `DbContext`. The base class simply wires up interceptors and global filters automatically.

## 2. Register in DI

```csharp
builder.Services.AddEfCoreKit<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableSoftDelete()
        .EnableAuditTrail()
        .EnableMultiTenancy()
        .UseUserProvider<HttpContextUserProvider>()
        .UseTenantProvider<HttpContextTenantProvider>()
        .LogSlowQueries(TimeSpan.FromSeconds(1))
);

// Register your database provider for bulk operations
builder.Services.AddEfCoreKitSqlServer();
```

Each `Enable*()` call is opt-in — only the features you enable are active.

## 3. Implement Your Entities

Apply the interfaces for the features you want:

```csharp
public class Customer : IAuditable, ISoftDeletable, ITenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ITenantEntity
    public string? TenantId { get; set; }
}
```

You can implement any combination — an entity can be `IAuditable` only, or `ISoftDeletable` only, or all three, etc.

## 4. Implement IUserProvider

EfCoreKit needs to know _who_ the current user is for audit fields. Implement `IUserProvider`:

```csharp
public class HttpContextUserProvider : IUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextUserProvider(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string? GetCurrentUserId()
        => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? GetCurrentUserName()
        => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
}
```

## 5. Implement ITenantProvider (if using multi-tenancy)

```csharp
public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string? GetCurrentTenantId()
        => _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
}
```

## What Happens Automatically

Once configured, EfCoreKit handles the following behind the scenes via EF Core interceptors:

| Feature | What happens | When |
|---------|-------------|------|
| **Audit Trail** | Sets `CreatedAt`/`CreatedBy` on insert, `UpdatedAt`/`UpdatedBy` on update | Every `SaveChanges` / `SaveChangesAsync` |
| **Soft Delete** | Converts `DELETE` to `UPDATE SET IsDeleted = true` | When deleting an `ISoftDeletable` entity |
| **Multi-Tenancy** | Auto-assigns `TenantId` on insert, validates ownership on update | Every `SaveChanges` / `SaveChangesAsync` |
| **Query Filters** | Hides soft-deleted rows and scopes queries to the current tenant | Every LINQ query |
| **Slow Query Logging** | Logs a warning for queries exceeding the threshold | After each database command |

## Next Steps

- [Soft Delete](soft-delete.md) — How soft delete works, cascade delete, querying deleted records
- [Audit Trail](audit-trail.md) — Automatic timestamps and user tracking
- [Multi-Tenancy](multi-tenancy.md) — Tenant isolation and filtering
- [Pagination](pagination.md) — Offset and keyset/cursor pagination
- [Query Helpers](query-helpers.md) — Conditional filtering, dynamic ordering, projections, specifications
- [Bulk Operations](bulk-operations.md) — High-performance batch insert, update, delete, upsert

