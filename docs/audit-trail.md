# Audit Trail

EfCoreKit automatically timestamps entity creation and modification, and records which user made the change. You can optionally enable a full field-level change history log.

## How It Works

The `AuditInterceptor` hooks into every `SaveChanges` / `SaveChangesAsync` call:

| State | What happens |
|-------|-------------|
| `Added` | Sets `CreatedAt` to `DateTime.UtcNow` and `CreatedBy` to the current user ID |
| `Modified` | Sets `UpdatedAt` to `DateTime.UtcNow` and `UpdatedBy` to the current user ID. Protects `CreatedAt` and `CreatedBy` from being overwritten |

## Setup

### Basic Audit (timestamps only)

```csharp
builder.Services.AddEfCoreExtensions<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableAuditTrail()
        .UseUserProvider<HttpContextUserProvider>());
```

### Full Audit Log (field-level change history)

```csharp
kit.EnableAuditTrail(fullLog: true)
```

When `fullLog: true`, the interceptor also writes an `AuditLog` row for every changed property on any `IFullAuditable` entity. This requires a `DbSet<AuditLog>` on your context:

```csharp
public class AppDbContext : EfCoreDbContext<AppDbContext>
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    // ...
}
```

## Implement the Interface

### IAuditable — timestamps only

Inherit `AuditableEntity` (or `AuditableEntity<TKey>`):

```csharp
public class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

Or implement the interface directly:

```csharp
public class Product : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

### IFullAuditable — timestamps + field-level history

Use `IFullAuditable` on entities where you want a complete change history:

```csharp
public class Invoice : AuditableEntity, IFullAuditable { }
```

`IFullAuditable` extends `IAuditable` — no extra properties are needed on your entity. The change history is stored in the separate `AuditLog` table.

## AuditLog Record

Each changed property on an `IFullAuditable` entity produces one `AuditLog` row:

| Column | Description |
|--------|-------------|
| `EntityType` | Class name of the modified entity |
| `EntityKey` | Primary key value(s) |
| `PropertyName` | Name of the changed property |
| `OldValue` | Value before the change (`null` for Added) |
| `NewValue` | Value after the change (`null` for Deleted) |
| `Action` | `"Added"`, `"Modified"`, or `"Deleted"` |
| `ChangedAt` | UTC timestamp of the save |
| `ChangedBy` | User ID from `IUserProvider` |

### Querying the Audit Log

```csharp
// Who changed invoice 42, and what?
var history = await context.AuditLogs
    .Where(a => a.EntityType == nameof(Invoice) && a.EntityKey == "42")
    .OrderBy(a => a.ChangedAt)
    .ToListAsync();
```

## Basic Usage

No extra code needed — just save normally:

```csharp
// Insert — CreatedAt and CreatedBy are set automatically
var product = new Product { Name = "Widget", Price = 9.99m };
context.Products.Add(product);
await context.SaveChangesAsync();
// product.CreatedAt == DateTime.UtcNow
// product.CreatedBy == "user-123"

// Update — UpdatedAt and UpdatedBy are set automatically
product.Price = 12.99m;
await context.SaveChangesAsync();
// product.UpdatedAt == DateTime.UtcNow
// product.CreatedAt remains unchanged (protected)
```

## IUserProvider

EfCoreKit resolves the current user through `IUserProvider`. You provide the implementation:

```csharp
public interface IUserProvider
{
    string? GetCurrentUserId();
    string? GetCurrentUserName();
}
```

### ASP.NET Core

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

### Background Services

```csharp
public class SystemUserProvider : IUserProvider
{
    public string? GetCurrentUserId() => "system";
    public string? GetCurrentUserName() => "Background Service";
}
```

## CreatedAt / CreatedBy Protection

The audit interceptor marks `CreatedAt` and `CreatedBy` as `IsModified = false` on every update. Even if your code accidentally sets these properties during an update, the original values are preserved in the database.

## Combining with Soft Delete

When an entity implements both `IAuditable` and `ISoftDeletable`, a soft delete triggers a `Modified` state change — so `UpdatedAt` and `UpdatedBy` are stamped at the moment of deletion.
