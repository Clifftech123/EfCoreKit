# DbContext Utilities

EfCore.Extensions adds utility extension methods to `DbContext` for transactions, tracking management, and table maintenance.

## ExecuteInTransactionAsync

Wraps work in a database transaction while respecting EF Core's execution strategy (automatic retries on transient failures). Use this instead of manually calling `BeginTransactionAsync`.

### Void (fire-and-forget result)

```csharp
await context.ExecuteInTransactionAsync(async () =>
{
    await DoWorkA(context);
    await DoWorkB(context);
    await context.SaveChangesAsync();
});
```

### With Return Value

```csharp
var orderId = await context.ExecuteInTransactionAsync(async () =>
{
    var order = new Order { CustomerId = customerId, Total = total };
    context.Orders.Add(order);
    await context.SaveChangesAsync();
    return order.Id;
});
```

### Why Not BeginTransactionAsync Directly?

EF Core's execution strategy (e.g. SQL Server retry on transient fault) does not work with manually opened transactions. `ExecuteInTransactionAsync` uses `IExecutionStrategy.ExecuteInTransactionAsync` under the hood, so retries work correctly.

---

## DetachAll

Detaches all tracked entities from the change tracker. Useful after bulk import operations where you want to free memory and avoid stale entity conflicts.

```csharp
await BulkImportAsync(context, records);
context.DetachAll();
// Change tracker is now empty
```

---

## TruncateAsync&lt;T&gt;

Issues a `TRUNCATE TABLE` statement for the table mapped to entity type `T`. The table name is resolved using EF Core metadata — no hard-coded strings.

```csharp
// Remove all rows from the AuditLog table (fast, no row-by-row DELETE)
await context.TruncateAsync<AuditLog>();
```

> **Warning:** `TRUNCATE TABLE` is not transactional on all databases and cannot be rolled back in some scenarios. Use with care and only when you are certain you want to remove all rows permanently.

---

## Summary

| Method | Purpose |
|--------|---------|
| `ExecuteInTransactionAsync(Func<Task>)` | Run work in a transaction with execution-strategy support |
| `ExecuteInTransactionAsync<T>(Func<Task<T>>)` | Same, returning a value |
| `DetachAll()` | Clear the change tracker |
| `TruncateAsync<T>()` | Truncate a table by entity type |
