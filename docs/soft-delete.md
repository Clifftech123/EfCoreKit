# Soft Delete

Soft delete marks records as deleted instead of physically removing them from the database. This preserves data history and allows recovery.

## How It Works

1. **Interceptor** — When you call `context.Remove(entity)` or set `EntityState.Deleted`, the `SoftDeleteInterceptor` intercepts the operation, changes the state to `Modified`, and sets `IsDeleted = true`, `DeletedAt`, and `DeletedBy`.
2. **Global Query Filter** — A query filter is automatically applied so that `WHERE IsDeleted = false` is added to every query. Soft-deleted rows are invisible by default.

Both sync (`SaveChanges`) and async (`SaveChangesAsync`) paths are handled.

## Setup

```csharp
builder.Services.AddEfCoreKit<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableSoftDelete(cascade: true) // cascade: optional, soft-deletes child entities too
        .UseUserProvider<HttpContextUserProvider>()
);
```

## Implement the Interface

Add `ISoftDeletable` to any entity you want to soft-delete:

```csharp
public class Order : ISoftDeletable
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

## Deleting Records

Use normal EF Core delete operations — the interceptor handles the rest:

```csharp
// These both trigger soft delete automatically
context.Orders.Remove(order);
await context.SaveChangesAsync();

// Or via state change
context.Entry(order).State = EntityState.Deleted;
await context.SaveChangesAsync();
```

After saving, the record still exists in the database with `IsDeleted = true`.

## Querying

Soft-deleted records are automatically excluded from all queries:

```csharp
// Only returns non-deleted orders
var orders = await context.Orders.ToListAsync();
```

### Include Deleted Records

Use `IgnoreQueryFilters()` to bypass the filter:

```csharp
// All orders, including soft-deleted ones
var allOrders = await context.Orders
    .IgnoreQueryFilters()
    .ToListAsync();

// Only deleted orders
var deletedOrders = await context.Orders
    .IgnoreQueryFilters()
    .Where(o => o.IsDeleted)
    .ToListAsync();
```

## Cascade Soft Delete

When `cascade: true` is enabled, loaded child navigation properties that implement `ISoftDeletable` are also soft-deleted:

```csharp
kit.EnableSoftDelete(cascade: true);
```

```csharp
// Deleting an order also soft-deletes its loaded OrderItems
var order = await context.Orders
    .Include(o => o.Items)
    .FirstAsync(o => o.Id == orderId);

context.Orders.Remove(order);
await context.SaveChangesAsync();
// order.IsDeleted == true
// order.Items.All(i => i.IsDeleted) == true
```

> **Important:** Cascade soft delete only affects navigation properties that are **loaded** (included) in the change tracker. Unloaded relations are not affected.

## Restoring Deleted Records

To restore a soft-deleted entity, query it with `IgnoreQueryFilters()` and set `IsDeleted = false`:

```csharp
var order = await context.Orders
    .IgnoreQueryFilters()
    .FirstAsync(o => o.Id == orderId);

order.IsDeleted = false;
order.DeletedAt = null;
order.DeletedBy = null;
await context.SaveChangesAsync();
```

