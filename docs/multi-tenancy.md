# Multi-Tenancy

EfCoreKit provides automatic tenant isolation so each tenant only sees and modifies their own data.

## How It Works

Two mechanisms work together:

1. **TenantInterceptor** — On insert, automatically assigns `TenantId` from `ITenantProvider`. On update, validates the entity belongs to the current tenant and prevents `TenantId` from being changed.
2. **TenantQueryFilter** — A global query filter adds `WHERE TenantId = @currentTenantId` to every query. The tenant ID is resolved at query execution time, not at model build time.

## Setup

```csharp
builder.Services.AddEfCoreKit<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableMultiTenancy()
        .UseTenantProvider<HttpContextTenantProvider>()
);
```

## Implement the Interface

Add `ITenantEntity` to entities that should be scoped per tenant:

```csharp
public class Invoice : ITenantEntity
{
    public int Id { get; set; }
    public decimal Amount { get; set; }

    // ITenantEntity
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

### ASP.NET Core Example

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

### Header-Based Tenant Resolution

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
// invoice.TenantId == "tenant-abc" (from ITenantProvider, assigned automatically)
```

If `TenantId` is already set on the entity, the interceptor won't overwrite it.

### Update — Tenant Ownership Validation

```csharp
// This throws TenantMismatchException if the invoice belongs to a different tenant
var invoice = await context.Invoices.FindAsync(invoiceId);
invoice.Amount = 200.00m;
await context.SaveChangesAsync(); // Validates TenantId matches current tenant
```

The interceptor also prevents `TenantId` from being changed during updates.

### Query — Automatic Tenant Filtering

```csharp
// Only returns invoices for the current tenant
var invoices = await context.Invoices.ToListAsync();
// SQL: SELECT * FROM Invoices WHERE TenantId = 'tenant-abc' AND ...
```

### Bypass Tenant Filter (Admin Queries)

```csharp
// Returns all invoices across all tenants
var allInvoices = await context.Invoices
    .IgnoreQueryFilters()
    .ToListAsync();
```

## TenantMismatchException

If a user tries to modify an entity belonging to a different tenant, a `TenantMismatchException` is thrown:

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

Multi-tenancy works alongside soft delete and audit trail. When all three are enabled on an entity:

- **Insert:** `TenantId`, `CreatedAt`, `CreatedBy` are all set automatically
- **Update:** Tenant ownership is validated, `UpdatedAt`/`UpdatedBy` are set
- **Delete:** Soft delete is applied, tenant is validated, and audit fields are updated
- **Query:** Both `IsDeleted = false` and `TenantId = @current` filters are applied

