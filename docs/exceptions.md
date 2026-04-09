# Exceptions

All exceptions thrown by EfCoreKit inherit from `EfCoreException` — catch the base type to handle any library error in one place, or catch specific subtypes for finer-grained handling.

## Exception Hierarchy

```
Exception
└── EfCoreException                 (abstract base)
    ├── EntityNotFoundException
    ├── ConcurrencyConflictException
    ├── DuplicateEntityException
    └── InvalidFilterException
```

---

## EfCoreException

Abstract base class for all EfCoreKit exceptions.

```csharp
try
{
    await context.SaveChangesAsync();
}
catch (EfCoreException ex)
{
    // Catches any exception thrown by EfCoreKit
    logger.LogError(ex, "Data access error");
}
```

---

## EntityNotFoundException

Thrown by `GetByIdOrThrowAsync` and `RemoveByIdAsync` when the requested entity does not exist.

```csharp
public sealed class EntityNotFoundException : EfCoreException
{
    public string  EntityType { get; }  // class name of the entity
    public object? EntityId   { get; }  // the key that was looked up
}
```

```csharp
try
{
    var order = await context.Orders.GetByIdOrThrowAsync(orderId);
}
catch (EntityNotFoundException ex)
{
    // ex.EntityType == "Order"
    // ex.EntityId   == orderId
    return NotFound($"{ex.EntityType} {ex.EntityId} not found.");
}
```

Triggered by:
- `DbSet.GetByIdOrThrowAsync(id)`
- `IRepository<T>.GetByIdOrThrowAsync(id)` and `RemoveByIdAsync(id)`

---

## ConcurrencyConflictException

Thrown automatically by `EfCoreDbContext.SaveChangesAsync` when a `DbUpdateConcurrencyException` is detected (stale row-version conflict with `IConcurrencyAware` entities).

```csharp
public sealed class ConcurrencyConflictException : EfCoreException
{
    public string  EntityType { get; }  // class name of the conflicting entity
    public object? EntityId   { get; }  // primary key of the conflicting row
}
```

```csharp
try
{
    await context.SaveChangesAsync();
}
catch (ConcurrencyConflictException ex)
{
    // ex.EntityType == "Order"
    // ex.EntityId   == 42
    return Conflict($"'{ex.EntityType}' was modified by another user. Please reload and retry.");
}
```

To use concurrency control, add `IConcurrencyAware` (or use `FullEntity`) and configure `RowVersion` in EF Core:

```csharp
public class Order : AuditableEntity, IConcurrencyAware
{
    public byte[] RowVersion { get; set; } = [];
}

// In your entity configuration:
builder.Property(o => o.RowVersion).IsRowVersion();
```

---

## DuplicateEntityException

Not thrown automatically — throw it manually in your application when you catch a unique constraint violation from EF Core:

```csharp
public sealed class DuplicateEntityException : EfCoreException
{
    public string  EntityName { get; }   // entity type name
    public string? FieldName  { get; }   // unique field (null if unknown)
    public object? FieldValue { get; }   // the duplicate value (null if unknown)
}
```

```csharp
try
{
    await context.SaveChangesAsync();
}
catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
{
    throw new DuplicateEntityException("Customer", "Email", email);
}
```

Messages produced:
- `new DuplicateEntityException("Customer")` → `"A Customer with the same unique key already exists."`
- `new DuplicateEntityException("Customer", "Email")` → `"A Customer with the same 'Email' already exists."`
- `new DuplicateEntityException("Customer", "Email", email)` → `"A Customer with Email = 'jane@example.com' already exists."`

---

## InvalidFilterException

Thrown by `ApplyFilters` when a `FilterDescriptor` is invalid.

```csharp
public sealed class InvalidFilterException : EfCoreException { }
```

| Cause | Message |
|-------|---------|
| `Field` is null or whitespace | `"Filter field name cannot be null or empty."` |
| Unsupported operator | `"Unsupported filter operator: 'xyz'."` |
| `in` value is not `IEnumerable` | `"'in' operator requires an IEnumerable value."` |
| `between` value is not `object[2]` | `"'between' operator requires a 2-element object[] value: [min, max]."` |

```csharp
try
{
    var results = await context.Products.ApplyFilters(filters).ToListAsync();
}
catch (InvalidFilterException ex)
{
    return BadRequest(ex.Message);
}
```

---

**Previous:** [← DbContext Utilities](dbcontext-utilities.md)
