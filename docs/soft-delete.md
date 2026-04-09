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
        .EnableSoftDelete()                  // soft delete only
        .EnableSoftDelete(cascade: true)     // also soft-deletes loaded child entities
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

    public bool      IsDeleted  { get; set; }
    public DateTime? DeletedAt  { get; set; }
    public string?   DeletedBy  { get; set; }
}
```

## Deleting Records

Use normal EF Core delete operations — the interceptor handles the rest:

```csharp
context.Orders.Remove(order);
await context.SaveChangesAsync();
// order.IsDeleted == true
// order.DeletedAt == DateTime.UtcNow
// order.DeletedBy == "user-123"  (from IUserProvider)
```

The row stays in the database — it is just invisible to normal queries.

---

## Querying

### Default — Active Records Only

```csharp
var orders = await context.Orders.ToListAsync(); // soft-deleted rows excluded
```

### `GetDeletedAsync` — DbSet Method

Returns all soft-deleted rows directly from a `DbSet<T>`:

```csharp
IReadOnlyList<Order> trash = await context.Orders.GetDeletedAsync();
```

> Requires `T : ISoftDeletable`. Bypasses the global filter and adds `WHERE IsDeleted = true`.

### `IncludeDeleted` — IQueryable Method

Returns active and soft-deleted rows together (bypasses global filter):

```csharp
var all = await context.Orders.IncludeDeleted().ToListAsync();

// Chain other LINQ operators freely
var allForCustomer = await context.Orders
    .IncludeDeleted()
    .Where(o => o.CustomerId == id)
    .OrderBy(o => o.CreatedAt)
    .ToListAsync();
```

### `OnlyDeleted` — IQueryable Method

Returns only soft-deleted rows (bypasses global filter and adds `WHERE IsDeleted = true`):

```csharp
var deletedOrders = await context.Orders.OnlyDeleted().ToListAsync();
```

---

## Restoring Records

Load the deleted record (bypassing the filter), then restore it:

```csharp
var order = await context.Orders
    .OnlyDeleted()
    .FirstAsync(o => o.Id == orderId);

context.Orders.Restore(order);
await context.SaveChangesAsync();
// order.IsDeleted == false, order.DeletedAt == null, order.DeletedBy == null
```

`Restore` clears `IsDeleted`, `DeletedAt`, and `DeletedBy` on the entity. Call `SaveChangesAsync` to persist.

---

## Hard Deleting Records

Use `HardDelete` to permanently remove a record, bypassing the soft-delete interceptor:

```csharp
context.Orders.HardDelete(order);
await context.SaveChangesAsync(); // row physically removed
```

Useful for GDPR erasure or clearing obsolete data when you want the row gone completely.

---

## Cascade Soft Delete

When `cascade: true` is set, loaded child navigation properties that also implement `ISoftDeletable` are soft-deleted in the same `SaveChanges` call:

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

> **Important:** cascade soft delete only affects navigation properties that are **loaded** (included) in the change tracker. Unloaded relations are not affected.

---

## Combining with Audit Trail

When an entity implements both `IAuditable` and `ISoftDeletable` (as `SoftDeletableEntity` does), a soft delete triggers a `Modified` state change — so `UpdatedAt` and `UpdatedBy` are also stamped at the moment of deletion.

---

[← Base Entities](base-entities.md) | [Audit Trail →](audit-trail.md)
