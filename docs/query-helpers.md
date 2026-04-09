# Query Helpers

EfCoreKit provides extension methods for `DbSet<T>` and `IQueryable<T>` that reduce boilerplate for common data access patterns. All native EF Core LINQ operators continue to work alongside these helpers.

---

## DbSet Extensions

### Single-Entity Lookups

```csharp
// By primary key — returns null if not found (checks change tracker first)
Order? order = await context.Orders.GetByIdAsync(orderId);

// By primary key — throws EntityNotFoundException if not found
Order order = await context.Orders.GetByIdOrThrowAsync(orderId);

// First matching, or null
Customer? customer = await context.Customers
    .FirstOrDefaultAsync(c => c.Email == "jane@example.com");

// Single matching (throws InvalidOperationException if >1), or null
Customer? customer = await context.Customers
    .SingleOrDefaultAsync(c => c.TaxId == taxId);

// Last matching, or null (query must be ordered)
Order? latest = await context.Orders
    .LastOrDefaultAsync(o => o.CustomerId == customerId);
```

### Multi-Entity Lookups

```csharp
// All entities (avoid on large tables)
var allCategories = await context.Categories.GetAllAsync();

// By predicate
var orders = await context.Orders.FindAsync(o => o.CustomerId == id);

// By specification
var orders = await context.Orders.FindAsync(new ActiveOrdersSpec(customerId));

// Multiple entities by key — translates to a single WHERE key IN (...) query
var customers = await context.Customers
    .GetByIdsAsync(c => c.Id, new[] { 1, 2, 3 });
```

> **`GetByIdsAsync` signature:** `GetByIdsAsync<T, TKey>(keySelector, ids)` — the key selector expression is required.

### Existence Checks

```csharp
// True if any entity exists at all
bool hasOrders = await context.Orders.AnyAsync();

// True if any entity matches the predicate
bool hasActive  = await context.Customers.ExistsAsync(c => c.IsActive);
bool hasAnything = await context.Products.AnyAsync(p => p.Price > 100);
```

> **`ExistsAsync` vs `AnyAsync`:** both work. `ExistsAsync(id)` checks by primary key and hits the change tracker first. `ExistsAsync(predicate)` / `AnyAsync(predicate)` translate to SQL `EXISTS`.

### Counting

```csharp
// All rows, or filtered
int  count    = await context.Products.CountAsync();
int  active   = await context.Products.CountAsync(p => p.IsActive);

// Long count for tables that may exceed int.MaxValue
long big      = await context.AuditLogs.LongCountAsync();
long errors   = await context.AuditLogs.LongCountAsync(l => l.Level == "Error");
```

### Aggregates

```csharp
// Max / Min
decimal highest = await context.Orders.MaxAsync(o => o.Total);
decimal lowest  = await context.Orders.MinAsync(o => o.Total);

// Sum — overloads for decimal, int, long, double
decimal revenue  = await context.Orders.SumAsync(o => o.Total);           // decimal
int     totalQty = await context.OrderItems.SumAsync(i => i.Quantity);    // int
long    events   = await context.Events.SumAsync(e => e.Count);           // long
double  score    = await context.Results.SumAsync(r => r.WeightedScore);  // double

// Average — overloads for int, decimal, double
double  avgQty   = await context.OrderItems.AverageAsync(i => i.Quantity); // int selector → double
decimal avgPrice = await context.Products.AverageAsync(p => p.Price);      // decimal
double  avgScore = await context.Results.AverageAsync(r => r.Score);       // double
```

### Write Operations

```csharp
// Stage for deletion by predicate (loads rows first, then calls RemoveRange)
await context.Orders.RemoveRangeAsync(o => o.Status == OrderStatus.Cancelled);
await context.SaveChangesAsync();
```

> **Note:** `RemoveRangeAsync` takes an `Expression<Func<T, bool>>` predicate, not a list. It loads the matching rows then stages them for deletion.

---

## Soft Delete Lifecycle

See [Soft Delete](soft-delete.md) for full details.

```csharp
// Load all soft-deleted rows directly (DbSet method)
var trash = await context.Orders.GetDeletedAsync();

// IQueryable: include deleted alongside active
var all = await context.Orders.IncludeDeleted().ToListAsync();

// IQueryable: only deleted rows
var deleted = await context.Orders.OnlyDeleted().ToListAsync();

// Restore a soft-deleted record (clears IsDeleted, DeletedAt, DeletedBy)
context.Orders.Restore(order);
await context.SaveChangesAsync();

// Permanently remove a record (bypasses soft-delete interceptor)
context.Orders.HardDelete(order);
await context.SaveChangesAsync();
```

---

## IQueryable Extensions

### Conditional Filtering

Apply filters only when a condition is true — ideal for optional search parameters:

```csharp
var results = await context.Products
    .WhereIf(hasCategory,     p => p.CategoryId == categoryId)
    .WhereIfNotNull(minPrice, p => p.Price >= minPrice)
    .WhereIfNotEmpty(search,  p => p.Name.Contains(search!))
    .ToListAsync();
```

| Method | Applies filter when... |
|--------|----------------------|
| `WhereIf(bool, predicate)` | The boolean is `true` |
| `WhereIfNotNull(value, predicate)` | The value is not `null` |
| `WhereIfNotEmpty(string, predicate)` | The string is not null, empty, or whitespace |

### Dynamic Ordering

Sort by a property name received from an API request:

```csharp
// Single column
var sorted = context.Products.OrderByDynamic("Price", ascending: false);

// Nested property path
var sorted = context.Customers.OrderByDynamic("Address.City");

// Multi-column
var sorted = context.Products
    .OrderByDynamic("Category")
    .ThenByDynamic("Price", ascending: false);
```

### Dynamic Filter Descriptors

See [Dynamic Filters](dynamic-filters.md) for all operators and examples.

```csharp
var filters = new[]
{
    new FilterDescriptor { Field = "Status",    Operator = "eq",      Value = "Active" },
    new FilterDescriptor { Field = "Price",     Operator = "gte",     Value = 9.99m },
    new FilterDescriptor { Field = "Tags",      Operator = "in",      Value = new[] { "VIP", "Premium" } },
    new FilterDescriptor { Field = "Score",     Operator = "between", Value = new object[] { 10, 100 } },
};

var results = await context.Products.ApplyFilters(filters).ToListAsync();
```

### Sort Descriptors

```csharp
var sorts = new[]
{
    new SortDescriptor { Field = "Category", Ascending = true },
    new SortDescriptor { Field = "Price",    Ascending = false },
};

var sorted = context.Products.ApplySorts(sorts);
```

The first descriptor maps to `OrderBy`, subsequent ones to `ThenBy`.

### Projections

```csharp
// Project and materialise as a list
var dtos = await context.Customers
    .Where(c => c.IsActive)
    .SelectToListAsync(c => new CustomerDto { Name = c.Name, Email = c.Email });

// Project and take first (or null)
var email = await context.Customers
    .Where(c => c.Id == customerId)
    .SelectFirstOrDefaultAsync(c => c.Email);

// Project with pagination (only fetches the selected columns)
var page = await context.Customers
    .SelectToPagedAsync(c => new CustomerDto { Name = c.Name }, page: 1, pageSize: 20);

// Distinct projected values
List<string> cities = await context.Customers.SelectDistinctAsync(c => c.Address.City);
```

### No-Tracking Queries

```csharp
var reports = await context.Orders
    .WithNoTracking()
    .Where(o => o.CreatedAt >= startDate)
    .ToListAsync();
```

`WithNoTracking()` is an alias for `.AsNoTracking()`.

---

## Specification Pattern

See [Specification Pattern](specifications.md) for full details.

```csharp
public class ActiveHighValueCustomers : Specification<Customer>
{
    public ActiveHighValueCustomers(decimal minSpend)
    {
        AddCriteria(c => c.IsActive && c.TotalSpend >= minSpend);
        AddInclude(c => c.Orders);
        ApplyOrderByDescending(c => c.TotalSpend);
        ApplyThenBy(c => c.Name);
        ApplyPaging(skip: 0, take: 50);
        ApplyAsNoTracking();
    }
}

var spec = new ActiveHighValueCustomers(minSpend: 1000);
var customers = await context.Customers.FindAsync(spec);

// Compose with And / Or
var vip      = new VipCustomerSpec();
var combined = spec.And(vip);
```

### Inline Builder

```csharp
var spec = new SpecificationBuilder<Product>()
    .AddCriteria(p => p.IsActive)
    .AddInclude(p => p.Category)
    .ApplyOrderBy(p => p.Name)
    .ApplyPaging(skip: 0, take: 25)
    .ApplyAsNoTracking();

var products = await context.Products.ApplySpecification(spec).ToListAsync();
```

---

[← Dynamic Filters](dynamic-filters.md) | [DbContext Utilities →](dbcontext-utilities.md)
