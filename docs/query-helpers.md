# Query Helpers

EfCore.Extensions provides extension methods for `DbSet<T>` and `IQueryable<T>` that reduce boilerplate for common data access patterns. All native EF Core LINQ operators continue to work alongside these helpers.

## DbSet Extensions

### Single-Entity Lookups

```csharp
// Find by primary key (returns null if not found)
Order? order = await context.Orders.GetByIdAsync(orderId);

// Find by primary key (throws EntityNotFoundException if not found)
Order order = await context.Orders.GetByIdOrThrowAsync(orderId);

// First matching, or null
Customer? customer = await context.Customers
    .FirstOrDefaultAsync(c => c.Email == "jane@example.com");

// Single matching (throws if more than one match), or null
Customer? customer = await context.Customers
    .SingleOrDefaultAsync(c => c.Email == "jane@example.com");

// Last matching, or null
Order? latest = await context.Orders
    .LastOrDefaultAsync(o => o.CustomerId == customerId);
```

### Multi-Entity Lookups

```csharp
// All entities
var allCustomers = await context.Customers.GetAllAsync();

// Find by predicate
var orders = await context.Orders.FindAsync(o => o.CustomerId == id);

// Find by specification
var orders = await context.Orders.FindAsync(new ActiveOrdersSpec(customerId));

// Multiple entities by their IDs
var customers = await context.Customers.GetByIdsAsync([1, 2, 3]);
```

### Existence Checks

```csharp
bool hasActive = await context.Customers.ExistsAsync(c => c.IsActive);
bool any       = await context.Products.AnyAsync(p => p.Price > 100);
```

### Counting

```csharp
int  count    = await context.Orders.CountAsync(o => o.Status == OrderStatus.Active);
long bigCount = await context.Events.LongCountAsync(e => e.Year == 2026);
```

### Aggregates

```csharp
decimal max     = await context.Products.MaxAsync(p => p.Price);
decimal min     = await context.Products.MinAsync(p => p.Price);
int     total   = await context.OrderItems.SumAsync(i => i.Quantity);
double  average = await context.Products.AverageAsync(p => (double)p.Price);
```

### Soft Delete Lifecycle

See [Soft Delete](soft-delete.md) for full details. Quick reference:

```csharp
// Return active + deleted rows together
var all = await context.Customers.IncludeDeleted().ToListAsync();

// Return only deleted rows
var trash = await context.Customers.OnlyDeleted().ToListAsync();

// Restore a deleted record
context.Customers.Restore(customer);

// Permanently remove a record
context.Customers.HardDelete(customer);
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
var sorted = context.Products.OrderByDynamic("Price", ascending: false);

// Nested property path
var sorted = context.Customers.OrderByDynamic("Address.City");

// Multi-column sort
var sorted = context.Products
    .OrderByDynamic("Category")
    .ThenByDynamic("Price", ascending: false);
```

### Dynamic Filter Descriptors

See [Dynamic Filters](dynamic-filters.md) for full details. Quick reference:

```csharp
var filters = new[]
{
    new FilterDescriptor("Status",    "eq",      "Active"),
    new FilterDescriptor("Price",     "gte",     9.99m),
    new FilterDescriptor("Tags",      "in",      new[] { "VIP", "Premium" }),
    new FilterDescriptor("Score",     "between", new object[] { 10, 100 }),
};

var results = await context.Products.ApplyFilters(filters).ToListAsync();
```

### Projections

```csharp
// Project and materialize as a list
var dtos = await context.Customers
    .Where(c => c.IsActive)
    .SelectToListAsync(c => new CustomerDto { Name = c.Name, Email = c.Email });

// Project and take first
var email = await context.Customers
    .Where(c => c.Id == customerId)
    .SelectFirstOrDefaultAsync(c => c.Email);

// Project with pagination
var page = await context.Customers
    .SelectToPagedAsync(c => new CustomerDto { Name = c.Name }, page: 1, pageSize: 20);

// Distinct values
List<string> cities = await context.Customers.SelectDistinctAsync(c => c.Address.City);
```

### No-Tracking Queries

```csharp
var reports = await context.Orders
    .WithNoTracking()
    .Where(o => o.CreatedAt >= startDate)
    .ToListAsync();
```

---

## Specification Pattern

See [Specification Pattern](specifications.md) for full details. Quick reference:

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

// Use it directly on a DbSet
var spec = new ActiveHighValueCustomers(minSpend: 1000);
var customers = await context.Customers.FindAsync(spec);

// Or compose with And/Or
var highValue = new ActiveHighValueCustomers(500);
var vip       = new VipCustomerSpec();
var combined  = highValue.And(vip);
```

### Inline Builder

For one-off queries without creating a dedicated class:

```csharp
var spec = new SpecificationBuilder<Product>()
    .AddCriteria(p => p.IsActive)
    .AddInclude(p => p.Category)
    .ApplyOrderBy(p => p.Name)
    .ApplyPaging(skip: 0, take: 25)
    .ApplyAsNoTracking();

var products = await context.Products
    .ApplySpecification(spec)
    .ToListAsync();
```
