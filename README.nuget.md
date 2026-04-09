# EfCoreKit

EF Core extensions that eliminate boilerplate, so you can focus on building features.

**.NET 8 / 9 / 10** · **EF Core 8.x / 9.x / 10.x** · **Works with any EF Core-supported database**

---

## Why EfCoreKit?

Every .NET project with EF Core ends up writing the same plumbing: soft delete filters, audit timestamps, pagination helpers, generic repositories, transaction wrappers. EfCoreKit packages all of that into a single `AddEfCoreExtensions()` call.

- **Zero lock-in**  Uses standard EF Core interceptors and global query filters. Your entities stay plain C# classes and you can remove EfCoreKit at any time without rewriting your data layer.
- **Opt-in everything**  Enable only the features you need. Nothing runs unless you turn it on.
- **No custom ORM**  This is not a replacement for EF Core. It's a set of extensions that plug into the pipeline you already use.

---

## Features

| Feature | Description |
|---------|-------------|
| **Base Entity Hierarchy** | Ready-made base classes: `BaseEntity`, `AuditableEntity`, `SoftDeletableEntity`, `FullEntity` |
| **Entity Configuration Bases** | Fluent config base classes that auto-wire keys, indexes, and soft-delete defaults |
| **Soft Delete** | Mark records as deleted with automatic global query filters; restore or hard-delete on demand |
| **Audit Trail** | Auto-stamp `CreatedAt/By`, `UpdatedAt/By`; optional field-level `AuditLog` history |
| **Repository + Unit of Work** | Generic `IRepository<T>` / `IReadRepository<T>` backed by `IUnitOfWork` |
| **Specification Pattern** | Composable query specs with `And()` / `Or()` combinators, projection, and multi-column ordering |
| **Pagination** | Offset (`ToPagedAsync`) and keyset/cursor (`ToKeysetPagedAsync`) pagination with `PagedResult<T>` |
| **Dynamic Filters** | Apply runtime filter arrays (eq, ne, gt, lt, contains, in, between, isnull…) via `ApplyFilters` |
| **Query Helpers** | `ExistsAsync`, `GetByIdOrThrowAsync`, `WhereIf`, `OrderByDynamic`, and more |
| **DbContext Utilities** | `ExecuteInTransactionAsync`, `DetachAll`, `TruncateAsync<T>` |
| **Slow Query Logging** | Logs warnings for queries exceeding a configurable threshold |
| **Structured Exceptions** | `EntityNotFoundException`, `ConcurrencyConflictException`, `DuplicateEntityException`, `InvalidFilterException` |

---

## Installation

```bash
dotnet add package EfCoreKit
```

One package — everything is included. No separate installs needed.

---

## Quick Start

### 1. Register services

```csharp
builder.Services.AddEfCoreExtensions<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableSoftDelete()
        .EnableAuditTrail()
        .UseUserProvider<HttpContextUserProvider>()
        .LogSlowQueries(TimeSpan.FromSeconds(1)));
```

### 2. Inherit a base entity

```csharp
public class Product  : BaseEntity { }           // int PK
public class Order    : AuditableEntity<Guid> { } // audited, Guid PK
public class Customer : SoftDeletableEntity { }   // soft-deletable + audited
public class Invoice  : FullEntity { }            // soft-delete + audit + row version
```

### 3. Use the repository

```csharp
public class OrderService(IRepository<Order> repo, IUnitOfWork uow)
{
    public async Task<Order> CreateAsync(Order order)
    {
        await repo.AddAsync(order);
        await uow.CommitAsync();
        return order;
    }
}
```

### 4. Use specifications

```csharp
public class ActiveOrdersSpec : Specification<Order>
{
    public ActiveOrdersSpec(int customerId)
    {
        AddCriteria(o => o.CustomerId == customerId);
        AddInclude(o => o.Items);
        ApplyOrderByDescending(o => o.CreatedAt);
        ApplyPaging(skip: 0, take: 20);
        ApplyAsNoTracking();
    }
}

var orders = await repo.FindAsync(new ActiveOrdersSpec(customerId));
```

---

## Soft Delete

```csharp
var active  = await context.Customers.ToListAsync();           // deleted rows excluded
var all     = await context.Customers.IncludeDeleted().ToListAsync();
var deleted = await context.Customers.OnlyDeleted().ToListAsync();

context.Customers.Restore(customer);    // un-delete
context.Customers.HardDelete(customer); // permanent remove
await context.SaveChangesAsync();
```

---

## Pagination

```csharp
// Offset pagination
var page = await context.Orders
    .OrderBy(o => o.CreatedAt)
    .ToPagedAsync(page: 2, pageSize: 25);

// Keyset / cursor pagination
var first = await context.Orders
    .OrderBy(o => o.Id)
    .ToKeysetPagedAsync(o => o.Id, cursor: null, pageSize: 25);

var next = await context.Orders
    .OrderBy(o => o.Id)
    .ToKeysetPagedAsync(o => o.Id, cursor: int.Parse(first.NextCursor!), pageSize: 25);
```

---

## Dynamic Filters

```csharp
var filters = new[]
{
    new FilterDescriptor { Field = "Status",    Operator = "eq",      Value = "Active" },
    new FilterDescriptor { Field = "CreatedAt", Operator = "gte",     Value = DateTime.UtcNow.AddDays(-30) },
    new FilterDescriptor { Field = "Tags",      Operator = "in",      Value = new[] { "VIP", "Premium" } },
    new FilterDescriptor { Field = "Score",     Operator = "between", Value = new object[] { 10, 100 } },
};

var results = await context.Customers.ApplyFilters(filters).ToListAsync();
```

Supported operators: `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `contains`, `startswith`, `endswith`, `isnull`, `isnotnull`, `in`, `between`.

---

## Full Documentation

Complete guides, API reference, and examples are available on GitHub:
https://github.com/Clifftech123/EfCoreKit

---

## License

MIT — free for personal and commercial use, forever.
