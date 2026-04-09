# Getting Started

## Installation

One package — everything included:

```bash
dotnet add package EfCoreKit
```

## 1. Create Your DbContext

Inherit from `EfCoreDbContext<T>` instead of `DbContext`:

```csharp
using EfCoreKit.Core.Context;

public class AppDbContext : EfCoreDbContext<AppDbContext>
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>(); // required only when fullLog: true

    public AppDbContext(DbContextOptions<AppDbContext> options, EfCoreOptions efOptions)
        : base(options, efOptions) { }
}
```

> **Tip:** Inheriting from `EfCoreDbContext<T>` is optional — the interceptors and extensions work with any `DbContext`. The base class wires up interceptors and global query filters automatically.

## 2. Register in DI

```csharp
// Pick only the features you need — enable each option once
builder.Services.AddEfCoreExtensions<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableSoftDelete()
        .EnableAuditTrail()               // basic: stamps CreatedAt/By, UpdatedAt/By
        // .EnableAuditTrail(fullLog: true) // alternative: also writes field-level AuditLog rows
        .UseUserProvider<HttpContextUserProvider>()
        .LogSlowQueries(TimeSpan.FromSeconds(1)));
```

Each `Enable*()` call is opt-in — only the features you enable are active.

`AddEfCoreExtensions` also registers:
- `IRepository<T>` and `IReadRepository<T>` → backed by `Repository<T>`
- `IUnitOfWork` → backed by `UnitOfWork<TContext>`

## 3. Define Your Entities

The easiest path is to inherit a base class — they wire up the interface boilerplate for you:

```csharp
// Plain entity with int PK
public class Product : BaseEntity { }

// Audited (CreatedAt/By, UpdatedAt/By) with Guid PK
public class Order : AuditableEntity<Guid> { }

// Soft-deletable + audited, int PK
public class Customer : SoftDeletableEntity { }

// Full — soft-delete + audit + row version
public class Invoice : FullEntity { }
```

You can also implement interfaces directly if you prefer to control your own hierarchy:

```csharp
public class Customer : IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

## 4. Configure with Base Classes (optional)

Use the configuration base classes to auto-apply standard EF Core mappings:

```csharp
public class CustomerConfiguration : SoftDeletableEntityConfiguration<Customer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
```

| Base class | Auto-configures |
|-----------|----------------|
| `BaseEntityConfiguration<T, TKey>` | Primary key (`HasKey(e => e.Id)`) |
| `AuditableEntityConfiguration<T, TKey>` | Key + audit columns + index on `CreatedAt` |
| `SoftDeletableEntityConfiguration<T, TKey>` | Audit + `IsDeleted` default + composite index on `(IsDeleted, CreatedAt)` |

## 5. Implement IUserProvider

EfCoreKit needs to know who the current user is for audit fields. Implement `IUserProvider`:

```csharp
public class HttpContextUserProvider : IUserProvider
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextUserProvider(IHttpContextAccessor accessor) => _accessor = accessor;

    public string? GetCurrentUserId()
        => _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? GetCurrentUserName()
        => _accessor.HttpContext?.User?.Identity?.Name;
}
```

## What Happens Automatically

Once configured, EfCoreKit handles the following via EF Core interceptors:

| Feature | What happens | When |
|---------|-------------|------|
| **Audit Trail** | Sets `CreatedAt`/`CreatedBy` on insert, `UpdatedAt`/`UpdatedBy` on update | Every `SaveChanges` / `SaveChangesAsync` |
| **Soft Delete** | Converts `DELETE` to `UPDATE SET IsDeleted = true` | When deleting an `ISoftDeletable` entity |
| **Query Filters** | Hides soft-deleted rows | Every LINQ query |
| **Slow Query Logging** | Logs a warning for queries exceeding the threshold | After each database command |
| **Concurrency** | Throws `ConcurrencyConflictException` on stale row version conflicts | Every `SaveChanges` / `SaveChangesAsync` |

## Next Steps

- [Base Entities](base-entities.md) — Entity class hierarchy and configuration bases
- [Soft Delete](soft-delete.md) — Lifecycle methods, restoring records, cascade delete
- [Audit Trail](audit-trail.md) — Timestamps, user tracking, field-level AuditLog
- [Repository & Unit of Work](repository-uow.md) — Generic repository and transaction management
- [Specification Pattern](specifications.md) — Composable, reusable query logic
- [Pagination](pagination.md) — Offset and keyset/cursor pagination
- [Dynamic Filters](dynamic-filters.md) — Runtime filter arrays
- [Query Helpers](query-helpers.md) — WhereIf, OrderByDynamic, DbSet extensions
- [DbContext Utilities](dbcontext-utilities.md) — Transactions, DetachAll, TruncateAsync
- [Exceptions](exceptions.md) — All exception types, when they're thrown, what to catch

---

**Next:** [Base Entities →](base-entities.md)
