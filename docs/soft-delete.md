# Soft Delete

Soft delete marks records as deleted instead of physically removing them from the database. This preserves data history and allows recovery.

## How It Works

1. **Interceptor** — When you call `context.Remove(entity)` or set `EntityState.Deleted`, the `SoftDeleteInterceptor` intercepts the operation, changes the state to `Modified`, and sets `IsDeleted = true`, `DeletedAt`, and `DeletedBy`.
2. **Global Query Filter** — A query filter automatically adds `WHERE IsDeleted = false` to every query. Soft-deleted rows are invisible by default.

Both sync (`SaveChanges`) and async (`SaveChangesAsync`) paths are handled.

## Setup

```csharp
builder.Services.AddEfCoreExtensions<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableSoftDelete()
        .UseUserProvider<HttpContextUserProvider>());
```

## Implement the Interface

The easiest way is to inherit `SoftDeletableEntity` (or `SoftDeletableEntity<TKey>`):

```csharp
public class Order : SoftDeletableEntity
{
    public string Description { get; set; } = string.Empty;
}
```

Or implement `ISoftDeletable` directly:

```csharp
public class Order : ISoftDeletable
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

## Deleting Records

Use normal EF Core delete operations — the interceptor handles the rest:

```csharp
context.Orders.Remove(order);
await context.SaveChangesAsync();
// order.IsDeleted == true, order.DeletedAt == UtcNow, order.DeletedBy == "user-123"
```

The row stays in the database — it's just invisible to normal queries.

## Querying

### Default — Active Records Only

```csharp
var orders = await context.Orders.ToListAsync(); // soft-deleted rows excluded
```

### Include Deleted Alongside Active

```csharp
var all = await context.Orders.IncludeDeleted().ToListAsync();
```

### Only Deleted Records

```csharp
var trash = await context.Orders.OnlyDeleted().ToListAsync();
```

Both `IncludeDeleted()` and `OnlyDeleted()` are extension methods that apply `IgnoreQueryFilters()` and add the appropriate `Where` clause. They require `T : class, ISoftDeletable`.

## Restoring Deleted Records

```csharp
// Restore a soft-deleted record back to active
context.Orders.Restore(order);
await context.SaveChangesAsync();
// order.IsDeleted == false, order.DeletedAt == null, order.DeletedBy == null
```

To restore a record you need to load it first (bypassing the global filter):

```csharp
var order = await context.Orders
    .OnlyDeleted()
    .FirstAsync(o => o.Id == orderId);

context.Orders.Restore(order);
await context.SaveChangesAsync();
```

## Hard Deleting Records

Use `HardDelete` to permanently remove a record regardless of soft-delete settings:

```csharp
context.Orders.HardDelete(order);
await context.SaveChangesAsync(); // row is physically removed from the database
```

This bypasses the soft-delete interceptor and calls `DbSet.Remove` directly.

## Cascade Soft Delete

When `cascade: true` is set, loaded child navigation properties that implement `ISoftDeletable` are also soft-deleted:

```csharp
kit.EnableSoftDelete(cascade: true);
```

```csharp
var order = await context.Orders
    .Include(o => o.Items)
    .FirstAsync(o => o.Id == orderId);

context.Orders.Remove(order);
await context.SaveChangesAsync();
// order.IsDeleted == true
// order.Items[*].IsDeleted == true
```

> **Important:** Cascade soft delete only affects navigation properties that are **loaded** (included) in the change tracker.

## Combining with Audit Trail

When an entity implements both `IAuditable` and `ISoftDeletable` (e.g. `SoftDeletableEntity`), a soft delete triggers a `Modified` state change — so `UpdatedAt` and `UpdatedBy` are also stamped at the moment of deletion.

## Combining with Multi-Tenancy

Soft-deleted rows from other tenants remain invisible. The tenant filter and soft-delete filter are both applied independently, so you cannot accidentally see another tenant's deleted data.
