<div align="center">

# EfCoreKit

**EF Core extensions that eliminate boilerplate — so you can focus on building features.**

[![NuGet](https://img.shields.io/nuget/v/EfCoreKit?logo=nuget&label=NuGet)](https://www.nuget.org/packages/EfCoreKit)
[![NuGet Downloads](https://img.shields.io/nuget/dt/EfCoreKit?logo=nuget&label=Downloads)](https://www.nuget.org/packages/EfCoreKit)
[![Build](https://img.shields.io/github/actions/workflow/status/Clifftech123/EfCoreKit/ci.yml?branch=develop&logo=github&label=Build)](https://github.com/Clifftech123/EfCoreKit/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**Works with any EF Core-supported database**

</div>

---

## Why EfCoreKit?

Every .NET project with EF Core ends up writing the same plumbing: soft delete filters, audit timestamps, tenant isolation, pagination helpers, generic repositories, transaction wrappers. EfCoreKit packages all of that into a single `AddEfCoreExtensions()` call.

**Design goals:**

- **Zero lock-in**  Uses standard EF Core interceptors and global query filters. Your entities stay plain C# classes, your `DbContext` stays a normal `DbContext`, and you can remove EfCoreKit at any time without rewriting your data layer.
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

One package  everything is included. No separate installs needed.

---

## Quick Start

### 1. Register services

```csharp
builder.Services.AddEfCoreExtensions<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableSoftDelete()
        .EnableAuditTrail()               // basic: stamps CreatedAt/By, UpdatedAt/By
        // .EnableAuditTrail(fullLog: true) // alternative: also writes field-level AuditLog rows
        .UseUserProvider<HttpContextUserProvider>()
        .LogSlowQueries(TimeSpan.FromSeconds(1)));
```

### 2. Inherit a base entity

```csharp
// Plain entity with int PK
public class Product : BaseEntity { }

// Audited entity with Guid PK
public class Order : AuditableEntity<Guid> { }

// Soft-deletable + audited
public class Customer : SoftDeletableEntity { }

// Full — soft-delete + audit + row version
public class Invoice : FullEntity { }
```

### 3. Configure with base classes

```csharp
public class CustomerConfiguration : SoftDeletableEntityConfiguration<Customer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
    }
}
```

### 4. Use the repository

```csharp
public class OrderService(IRepository<Order> repo, IUnitOfWork uow)
{
    public async Task<Order> CreateAsync(Order order)
    {
        await repo.AddAsync(order);
        await uow.CommitAsync();
        return order;
    }

    public async Task<IReadOnlyList<Order>> GetRecentAsync()
        => await repo.FindAsync(o => o.CreatedAt > DateTime.UtcNow.AddDays(-7));
}
```

### 5. Use specifications

```csharp
public class ActiveOrdersSpec : Specification<Order>
{
    public ActiveOrdersSpec(int customerId)
    {
        AddCriteria(o => o.CustomerId == customerId && !o.IsDeleted);
        AddInclude(o => o.Items);
        ApplyOrderByDescending(o => o.CreatedAt);
        ApplyPaging(skip: 0, take: 20);
        ApplyAsNoTracking();
    }
}

// Compose specs with And/Or
var spec = new ActiveOrdersSpec(customerId).And(new HighValueOrdersSpec(500));
var orders = await dbSet.FindAsync(spec);
```

---

## Soft Delete Lifecycle

```csharp
// Default queries automatically exclude soft-deleted rows
var customers = await context.Customers.ToListAsync();

// Include soft-deleted rows alongside active ones
var all = await context.Customers.IncludeDeleted().ToListAsync();

// Only soft-deleted rows
var trash = await context.Customers.OnlyDeleted().ToListAsync();

// Restore a soft-deleted record
context.Customers.Restore(customer);
await context.SaveChangesAsync();

// Permanently remove a record (regardless of soft-delete settings)
context.Customers.HardDelete(customer);
await context.SaveChangesAsync();
```

---

## Pagination

```csharp
// Offset pagination
var page = await context.Orders
    .Where(o => o.CustomerId == id)
    .OrderBy(o => o.CreatedAt)
    .ToPagedAsync(page: 2, pageSize: 25);

Console.WriteLine($"Page {page.Page} of {page.TotalPages} ({page.TotalCount} total)");

// Keyset / cursor pagination (no OFFSET — scales to millions of rows)
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

var results = await context.Customers
    .ApplyFilters(filters)
    .ToListAsync();
```

Supported operators: `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `contains`, `startswith`, `endswith`, `isnull`, `isnotnull`, `in`, `between`.

---

## DbContext Utilities

```csharp
// Run work inside a transaction (respects EF Core execution strategy / retry)
var result = await context.ExecuteInTransactionAsync(async () =>
{
    await DoWorkA(context);
    await DoWorkB(context);
    return await context.SaveChangesAsync();
});

// Detach all tracked entities (useful after bulk imports)
context.DetachAll();

// Truncate a table by entity type (uses EF Core metadata for table name)
await context.TruncateAsync<AuditLog>();
```

---

## Documentation

| Guide | What You'll Learn |
|-------|-------------------|
| [Getting Started](docs/getting-started.md) | Installation, DbContext setup, DI registration |
| [Base Entities](docs/base-entities.md) | Entity hierarchy, configuration base classes |
| [Soft Delete](docs/soft-delete.md) | ISoftDeletable, lifecycle methods, restoring records |
| [Audit Trail](docs/audit-trail.md) | IAuditable, IFullAuditable, field-level AuditLog |
| [Repository & Unit of Work](docs/repository-uow.md) | IRepository, IReadRepository, IUnitOfWork |
| [Specification Pattern](docs/specifications.md) | Spec classes, And/Or combinators, projection specs |
| [Pagination](docs/pagination.md) | Offset and keyset pagination, PagedResult |
| [Dynamic Filters](docs/dynamic-filters.md) | FilterDescriptor, all supported operators |
| [Query Helpers](docs/query-helpers.md) | WhereIf, OrderByDynamic, DbSet extensions |
| [DbContext Utilities](docs/dbcontext-utilities.md) | Transactions, DetachAll, TruncateAsync |
| [Exceptions](docs/exceptions.md) | All exception types, when they're thrown, what to catch |

---

## Contributing

Contributions are welcome! Check out the [Contributing Guide](CONTRIBUTING.md) to get started.

- [Open an issue](https://github.com/Clifftech123/EfCoreKit/issues) to report a bug or suggest a feature
- [Start a discussion](https://github.com/Clifftech123/EfCoreKit/discussions) for questions or ideas
- [Submit a pull request](https://github.com/Clifftech123/EfCoreKit/pulls) — all PRs target the `develop` branch

---

## License

[MIT](LICENSE) — free for personal and commercial use, forever.
