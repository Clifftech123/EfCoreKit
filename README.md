<div align="center">

# EfCoreKit

**EF Core extensions that eliminate boilerplate — so you can focus on building features.**

[![NuGet](https://img.shields.io/nuget/v/EfCoreKit?logo=nuget&label=NuGet)](https://www.nuget.org/packages/EfCoreKit)
[![NuGet Downloads](https://img.shields.io/nuget/dt/EfCoreKit?logo=nuget&label=Downloads)](https://www.nuget.org/packages/EfCoreKit)
[![Build](https://img.shields.io/github/actions/workflow/status/Clifftech123/EfCoreKit/ci.yml?branch=develop&logo=github&label=Build)](https://github.com/Clifftech123/EfCoreKit/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**.NET 8 / 9 / 10** · **EF Core 8.x / 9.x / 10.x** · **Works with any EF Core-supported database**

</div>

---

## Why EfCoreKit?

Every .NET project with EF Core ends up writing the same plumbing: soft delete filters, audit timestamps, tenant isolation, pagination helpers, bulk imports. EfCoreKit packages all of that into a single `AddEfCoreKit()` call.

**Design goals:**

- **Zero lock-in** — EfCoreKit uses standard EF Core interceptors and global query filters. Your entities stay plain C# classes, your `DbContext` stays a normal `DbContext`, and you can remove EfCoreKit at any time without rewriting your data layer.
- **Opt-in everything** — Enable only the features you need. Nothing runs unless you turn it on.
- **No custom ORM** — This is not a repository layer or a replacement for EF Core. It's a set of extensions that plug into the pipeline you already use.

---

## Features

All core features use EF Core interceptors and global query filters — they work with **any database** EF Core supports.

| Feature | Status | Description |
|---------|--------|-------------|
| **Soft Delete** | ✅ | Mark records as deleted with automatic global query filters |
| **Audit Trail** | ✅ | Auto-stamp `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` on every save |
| **Multi-Tenancy** | ✅ | Automatic tenant filtering so each tenant only sees their own data |
| **Pagination** | ✅ | Offset-based and keyset/cursor-based pagination with `PagedResult<T>` |
| **Query Helpers** | ✅ | `ExistsAsync`, `GetByIdOrThrowAsync`, `WhereIf`, `OrderByDynamic`, and more |
| **Specification Pattern** | ✅ | Reusable, composable query logic in strongly-typed classes |
| **Slow Query Logging** | ✅ | Logs warnings for queries exceeding a configurable threshold |
| **Bulk Operations** | 🚧 | Insert, update, delete, or upsert thousands of rows in one call *(coming soon)* |

---

## Quick Look

```csharp
// One-line setup — pick only what you need
builder.Services.AddEfCoreKit<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableSoftDelete()
        .EnableAuditTrail()
        .EnableMultiTenancy()
        .UseUserProvider<HttpContextUserProvider>()
);
```

```csharp
// Query helpers
var exists   = await context.Customers.ExistsAsync(x => x.Email == email);
var customer = await context.Customers.GetByIdOrThrowAsync(id);

// Pagination
var page = await context.Customers
    .Where(c => c.IsActive)
    .ToPagedAsync(page: 1, pageSize: 20);

// Conditional filtering + dynamic sort
var results = await context.Orders
    .WhereIf(hasStatus, x => x.Status == status)
    .OrderByDynamic("CreatedAt", ascending: false)
    .ToListAsync();
```

### What happens behind the scenes

Once configured, EfCoreKit hooks into EF Core's pipeline automatically:

| You do this | EfCoreKit does this |
|-------------|---------------------|
| Call `SaveChangesAsync()` | Stamps `CreatedAt`/`UpdatedAt`, sets `CreatedBy`/`UpdatedBy` from your user provider |
| Delete an entity | Converts to a soft delete — sets `IsDeleted`, `DeletedAt`, `DeletedBy` instead of removing the row |
| Query any `DbSet` | Automatically filters out soft-deleted rows and scopes to the current tenant |
| Add a new tenant entity | Auto-assigns `TenantId` from your tenant provider |
| Modify a tenant entity you don't own | Throws `TenantMismatchException` before hitting the database |
| Run a slow query | Logs a warning with the SQL and duration so you can catch performance issues early |

---

## Installation

```bash
dotnet add package EfCoreKit
```

Or install only what you need:

| Package | Description |
|---------|-------------|
| `EfCoreKit.Core` | Core implementation (interceptors, filters, extensions) |
| `EfCoreKit.Abstractions` | Interfaces and models only |

---

## Documentation

Each feature has a dedicated guide with full examples and configuration options:

| Guide | What You'll Learn |
|-------|-------------------|
| [Getting Started](docs/getting-started.md) | Installation, DbContext setup, DI registration |
| [Soft Delete](docs/soft-delete.md) | ISoftDeletable, cascade delete, restoring records |
| [Audit Trail](docs/audit-trail.md) | IAuditable, auto-stamping, CreatedAt/CreatedBy protection |
| [Multi-Tenancy](docs/multi-tenancy.md) | ITenantEntity, automatic filtering, tenant validation |
| [Pagination](docs/pagination.md) | Offset and keyset pagination, PagedResult, KeysetPagedResult |
| [Query Helpers](docs/query-helpers.md) | WhereIf, OrderByDynamic, specifications, DbSet extensions |
| [Bulk Operations](docs/bulk-operations.md) | BulkInsert/Update/Delete/Upsert, BulkConfig tuning |

---

## Contributing

Contributions are welcome! Check out the [Contributing Guide](CONTRIBUTING.md) to get started.

- [Open an issue](https://github.com/Clifftech123/EfCoreKit/issues) to report a bug or suggest a feature
- [Start a discussion](https://github.com/Clifftech123/EfCoreKit/discussions) for questions or ideas
- [Submit a pull request](https://github.com/Clifftech123/EfCoreKit/pulls) — all PRs target the `develop` branch

---

## License

[MIT](LICENSE) — free for personal and commercial use, forever.
