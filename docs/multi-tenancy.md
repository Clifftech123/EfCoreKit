# Multi-Tenancy

EfCoreKit provides automatic tenant isolation so each tenant only sees and modifies their own data.

## How It Works

Two mechanisms work together:

1. **TenantInterceptor** — On insert, automatically assigns `TenantId` from `ITenantProvider`. On update, validates the entity belongs to the current tenant and prevents `TenantId` from being changed.
2. **TenantQueryFilter** — A global query filter adds `WHERE TenantId = @currentTenantId` to every query. The tenant ID is resolved at query execution time, not at model build time.

## Setup

```csharp
builder.Services.AddEfCoreExtensions<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableMultiTenancy()
        .UseTenantProvider<HttpContextTenantProvider>());
```

## Implement the Interface

Use `FullEntity` to get tenant support alongside audit trail and soft delete:

```csharp
public class Invoice : FullEntity { }
```

Or implement `ITenantEntity` directly on any existing entity:

```csharp
public class Invoice : ITenantEntity
{
    public int Id { get; set; }
    public decimal Amount { get; set; }

    public string? TenantId { get; set; }
}
```

## ITenantProvider

Implement `ITenantProvider` to tell EfCoreKit how to resolve the current tenant:

```csharp
public interface ITenantProvider
{
    string? GetCurrentTenantId();
}
```

### ASP.NET Core — From Claims

```csharp
public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextTenantProvider(IHttpContextAccessor accessor)
        => _accessor = accessor;

    public string? GetCurrentTenantId()
        => _accessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
}
```

### ASP.NET Core — From Header

```csharp
public class HeaderTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _accessor;

    public HeaderTenantProvider(IHttpContextAccessor accessor)
        => _accessor = accessor;

    public string? GetCurrentTenantId()
        => _accessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
}
```

## Automatic Behaviour

### Insert — TenantId Auto-Assignment

```csharp
var invoice = new Invoice { Amount = 100.00m };
context.Invoices.Add(invoice);
await context.SaveChangesAsync();
// invoice.TenantId == "tenant-abc" (assigned automatically from ITenantProvider)
```

If `TenantId` is already set on the entity, the interceptor won't overwrite it.

### Update — Tenant Ownership Validation

```csharp
// Throws TenantMismatchException if the invoice belongs to a different tenant
invoice.Amount = 200.00m;
await context.SaveChangesAsync();
```

The interceptor also prevents `TenantId` from being changed during updates.

### Query — Automatic Tenant Filtering

```csharp
// Only returns invoices for the current tenant
var invoices = await context.Invoices.ToListAsync();
// SQL: SELECT * FROM Invoices WHERE TenantId = 'tenant-abc'
```

### Bypass Tenant Filter (Admin Queries)

```csharp
var allInvoices = await context.Invoices
    .IgnoreQueryFilters()
    .ToListAsync(); // all tenants
```

## TenantMismatchException

If a user tries to modify an entity belonging to a different tenant, a `TenantMismatchException` is thrown before hitting the database:

```csharp
try
{
    await context.SaveChangesAsync();
}
catch (TenantMismatchException ex)
{
    // ex.ExpectedTenant — the current tenant from ITenantProvider
    // ex.ActualTenant   — the tenant on the entity
}
```

## Combining with Other Features

When soft delete, audit trail, and multi-tenancy are all enabled:

| Operation | What happens automatically |
|-----------|---------------------------|
| Insert | `TenantId`, `CreatedAt`, `CreatedBy` all set |
| Update | Tenant validated, `UpdatedAt`/`UpdatedBy` set |
| Delete | Soft deleted, tenant validated, audit fields updated |
| Query | Both `IsDeleted = false` and `TenantId = @current` filters applied |
