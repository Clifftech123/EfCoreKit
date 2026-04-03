# Query Helpers

EfCoreKit provides extension methods for `DbSet<T>` and `IQueryable<T>` that reduce boilerplate for common data access patterns. All native EF Core LINQ operators continue to work alongside these helpers.

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

// Multiple entities by their IDs
var customers = await context.Customers.GetByIdsAsync([1, 2, 3]);

// Find by composite key
var entity = await context.OrderItems.FindAsync(orderId, productId);
```

### Existence Checks

```csharp
// By primary key
bool exists = await context.Orders.ExistsAsync(orderId);

// By predicate
bool hasActive = await context.Customers.ExistsAsync(c => c.IsActive);

// Any matching
bool any = await context.Products.AnyAsync(p => p.Price > 100);
```

### Counting

```csharp
int count = await context.Orders.CountAsync(o => o.Status == OrderStatus.Active);
long bigCount = await context.Events.LongCountAsync(e => e.Year == 2026);
```

### Aggregates

```csharp
decimal maxPrice = await context.Products.MaxAsync(p => p.Price);
decimal minPrice = await context.Products.MinAsync(p => p.Price);

int totalQty = await context.OrderItems.SumAsync(i => i.Quantity);
decimal totalRevenue = await context.Orders.SumAsync(o => o.Total);

double avgPrice = await context.Products.AverageAsync(p => (double)p.Price);
```

### Write Operations

```csharp
await context.Products.AddRangeAsync(newProducts);
context.Products.UpdateRange(modifiedProducts);
context.Products.RemoveRangeAsync(expiredProducts);
```

## IQueryable Extensions

### Conditional Filtering

Apply filters only when a condition is true — ideal for optional search parameters:

```csharp
var results = await context.Products
    .WhereIf(hasCategory, p => p.CategoryId == categoryId)
    .WhereIfNotNull(minPrice, p => p.Price >= minPrice)
    .WhereIfNotEmpty(searchTerm, p => p.Name.Contains(searchTerm!))
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
// Simple property
var sorted = context.Products
    .OrderByDynamic("Price", ascending: false);

// Nested property path
var sorted = context.Customers
    .OrderByDynamic("Address.City");

// Multi-column sort
var sorted = context.Products
    .OrderByDynamic("Category")
    .ThenByDynamic("Price", ascending: false);
```

### FilterDescriptor — API-Driven Filtering

Apply filters from a list of descriptors (e.g. from a frontend grid component):

```csharp
var filters = new[]
{
    new FilterDescriptor { Field = "Status",  Operator = "eq",       Value = "Active" },
    new FilterDescriptor { Field = "Price",   Operator = "gte",      Value = 9.99m },
    new FilterDescriptor { Field = "Name",    Operator = "contains", Value = "Pro" }
};

var results = await context.Products
    .ApplyFilters(filters)
    .ToListAsync();
```

Supported operators: `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `contains`, `startswith`, `endswith`.

### SortDescriptor — API-Driven Sorting

```csharp
var sorts = new[]
{
    new SortDescriptor { Field = "Category", Ascending = true },
    new SortDescriptor { Field = "Price",    Ascending = false }
};

var sorted = context.Products.ApplySorts(sorts);
```

The first descriptor maps to `OrderBy`, subsequent ones to `ThenBy`.

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

// Project distinct values
List<string> cities = await context.Customers
    .SelectDistinctAsync(c => c.Address.City);
```

### No-Tracking Queries

```csharp
var reports = await context.Orders
    .WithNoTracking()
    .Where(o => o.CreatedAt >= startDate)
    .ToListAsync();
```

## Specification Pattern

Encapsulate reusable query logic in a class:

```csharp
public class ActiveHighValueCustomers : Specification<Customer>
{
    public ActiveHighValueCustomers(decimal minSpend)
    {
        AddCriteria(c => c.IsActive && c.TotalSpend >= minSpend);
        AddInclude(c => c.Orders);
        ApplyOrderByDescending(c => c.TotalSpend);
        ApplyPaging(skip: 0, take: 50);
        ApplyAsNoTracking();
    }
}

// Use it
var spec = new ActiveHighValueCustomers(minSpend: 1000);
var customers = await context.Customers
    .ApplySpecification(spec)
    .ToListAsync();
```

### Inline Builder

For one-off queries without creating a class:

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

### What ApplySpecification Does

A specification can set criteria, includes, ordering, paging, no-tracking, and split-query — all applied in one call:

| Property | Applied as |
|----------|-----------|
| `Criteria` | `.Where(criteria)` |
| `Includes` | `.Include(expression)` |
| `IncludeStrings` | `.Include("Navigation.Path")` |
| `OrderBy` | `.OrderBy(expression)` |
| `OrderByDescending` | `.OrderByDescending(expression)` |
| `Skip` / `Take` | `.Skip(n).Take(n)` |
| `AsNoTracking` | `.AsNoTracking()` |
| `AsSplitQuery` | `.AsSplitQuery()` |

