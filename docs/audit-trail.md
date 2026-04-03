# Audit Trail

EfCoreKit automatically timestamps entity creation and modification, and records which user made the change.

## How It Works

The `AuditInterceptor` hooks into every `SaveChanges` / `SaveChangesAsync` call:

| State | What happens |
|-------|-------------|
| `Added` | Sets `CreatedAt` to `DateTime.UtcNow` and `CreatedBy` to the current user ID |
| `Modified` | Sets `UpdatedAt` to `DateTime.UtcNow` and `UpdatedBy` to the current user ID. Protects `CreatedAt` and `CreatedBy` from being overwritten |

## Setup

```csharp
builder.Services.AddEfCoreKit<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableAuditTrail()
        .UseUserProvider<HttpContextUserProvider>()
);
```

## Implement the Interface

Add `IAuditable` to any entity you want to track:

```csharp
public class Product : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

## Usage

No extra code needed — just save normally:

```csharp
// Insert — CreatedAt and CreatedBy are set automatically
var product = new Product { Name = "Widget", Price = 9.99m };
context.Products.Add(product);
await context.SaveChangesAsync();
// product.CreatedAt == DateTime.UtcNow
// product.CreatedBy == "user-123" (from IUserProvider)

// Update — UpdatedAt and UpdatedBy are set automatically
product.Price = 12.99m;
await context.SaveChangesAsync();
// product.UpdatedAt == DateTime.UtcNow
// product.UpdatedBy == "user-123"
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

### ASP.NET Core Example

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

### Console App / Background Service Example

```csharp
public class SystemUserProvider : IUserProvider
{
    public string? GetCurrentUserId() => "system";
    public string? GetCurrentUserName() => "Background Service";
}
```

## CreatedAt / CreatedBy Protection

The audit interceptor marks `CreatedAt` and `CreatedBy` as `IsModified = false` on updates. This means even if your code accidentally sets these properties during an update, the original values are preserved in the database.

## Combining with Soft Delete

When an entity implements both `IAuditable` and `ISoftDeletable`, soft delete triggers a `Modified` state change, which means `UpdatedAt` and `UpdatedBy` are also set when an entity is soft-deleted.

