# EfCoreKit

A free, MIT-licensed EF Core extensions library that eliminates boilerplate so you can focus on building features.

## What It Does

EfCoreKit plugs into your existing `DbContext` and adds capabilities that most .NET projects need but have to rewrite every time:

- **Bulk Operations** — Insert, update, delete, or upsert thousands of records in one fast database call instead of N round trips
- **Soft Delete** — Mark records as deleted instead of removing them, with automatic global query filters so deleted rows are invisible by default
- **Audit Trail** — Auto-stamp `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` on every save, no manual code required
- **Pagination** — Offset-based and keyset/cursor-based pagination with a built-in `PagedResult<T>` model
- **Query Helpers** — `ExistsAsync`, `GetByIdAsync`, `GetByIdOrThrowAsync`, `WhereIf`, `OrderByDynamic` and more
- **Multi-Tenancy** — Automatic tenant filtering via global query filters so each tenant only sees their own data
- **Specification Pattern** — Reusable, composable query logic encapsulated in strongly-typed specification classes

## Supported Databases

| Database   | Version |
|------------|---------|
| SQL Server | 2016+   |
| PostgreSQL | 12+     |
| MySQL      | 8.0+    |
| SQLite     | 3.x     |

## Supported Platforms

| Platform | Version       |
|----------|---------------|
| .NET     | 8.0, 9.0, 10.0 |
| EF Core  | 8.x, 9.x, 10.x |

## Quick Look

```csharp
// Setup
builder.Services.AddEfCoreKit<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableSoftDelete()
        .EnableAuditTrail()
        .EnableMultiTenancy()
        .UseUserProvider<HttpContextUserProvider>()
);

// Bulk insert 10,000 rows in one call
await context.BulkInsertAsync(customers);

// Query helpers
var exists  = await context.Customers.ExistsAsync(x => x.Email == email);
var customer = await context.Customers.GetByIdOrThrowAsync(id);

// Pagination
var page = await context.Customers
    .Where(c => c.IsActive)
    .ToPagedAsync(page: 1, pageSize: 20);

// Conditional filtering
var results = await context.Orders
    .WhereIf(hasStatus, x => x.Status == status)
    .OrderByDynamic("CreatedAt", ascending: false)
    .ToListAsync();
```

## Packages

| Package                   | Description                        |
|---------------------------|------------------------------------|
| `EfCoreKit`               | Everything — all providers included |
| `EfCoreKit.Abstractions`  | Interfaces and models only          |
| `EfCoreKit.Core`          | Core implementation                 |
| `EfCoreKit.SqlServer`     | SQL Server bulk operations          |
| `EfCoreKit.PostgreSql`    | PostgreSQL bulk operations          |
| `EfCoreKit.MySql`         | MySQL bulk operations               |
| `EfCoreKit.Sqlite`        | SQLite bulk operations              |

Install just what you need, or grab the umbrella package:

```bash
dotnet add package EfCoreKit
```

## Documentation

Each feature has a dedicated guide with full examples, configuration options, and best practices:

| Guide | What You'll Learn |
|-------|-------------------|
| [Getting Started](docs/getting-started.md) | Installation, DbContext setup, DI registration |
| [Soft Delete](docs/soft-delete.md) | ISoftDeletable, cascade delete, restoring records |
| [Audit Trail](docs/audit-trail.md) | IAuditable, auto-stamping, CreatedAt/CreatedBy protection |
| [Multi-Tenancy](docs/multi-tenancy.md) | ITenantEntity, automatic filtering, tenant validation |
| [Pagination](docs/pagination.md) | Offset and keyset pagination, PagedResult, KeysetPagedResult |
| [Query Helpers](docs/query-helpers.md) | WhereIf, OrderByDynamic, specifications, DbSet extensions |
| [Bulk Operations](docs/bulk-operations.md) | BulkInsert/Update/Delete/Upsert, BulkConfig tuning |

## License

MIT — free for personal and commercial use, forever.
