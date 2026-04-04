# Specification Pattern

Specifications let you encapsulate query logic in strongly-typed, reusable classes — and compose them at runtime with `And` / `Or`.

## Creating a Specification

Inherit from `Specification<T>` and configure the query in the constructor:

```csharp
public class ActiveOrdersSpec : Specification<Order>
{
    public ActiveOrdersSpec(int customerId)
    {
        AddCriteria(o => o.CustomerId == customerId);
        AddCriteria(o => !o.IsDeleted);
        AddInclude(o => o.Items);
        ApplyOrderByDescending(o => o.CreatedAt);
        ApplyThenBy(o => o.Id);
        ApplyPaging(skip: 0, take: 20);
        ApplyAsNoTracking();
    }
}
```

## What You Can Configure

| Method | Applied as |
|--------|-----------|
| `AddCriteria(expr)` | `.Where(expr)` (multiple criteria are AND-ed together) |
| `AddInclude(expr)` | `.Include(expr)` |
| `AddInclude(string)` | `.Include("Navigation.Path")` |
| `ApplyOrderBy(expr)` | `.OrderBy(expr)` |
| `ApplyOrderByDescending(expr)` | `.OrderByDescending(expr)` |
| `ApplyThenBy(expr)` | `.ThenBy(expr)` |
| `ApplyThenByDescending(expr)` | `.ThenByDescending(expr)` |
| `ApplyPaging(skip, take)` | `.Skip(skip).Take(take)` |
| `ApplyAsNoTracking()` | `.AsNoTracking()` |
| `ApplyAsSplitQuery()` | `.AsSplitQuery()` |

## Using a Specification

### On a DbSet

```csharp
var orders = await context.Orders.FindAsync(new ActiveOrdersSpec(customerId));
```

### Via a Repository

```csharp
var orders = await repo.FindAsync(new ActiveOrdersSpec(customerId), ct);
```

### Manually via ApplySpecification

```csharp
var query = context.Orders.ApplySpecification(new ActiveOrdersSpec(customerId));
var orders = await query.ToListAsync();
```

---

## Composing Specifications with And / Or

Combine two specifications at runtime using the `And` / `Or` extension methods. The resulting spec merges both criteria expressions into a single expression tree — no intermediate materialisation.

```csharp
var active  = new ActiveOrdersSpec(customerId);
var highVal = new HighValueOrdersSpec(minTotal: 500);

// Orders that are active AND high value
var combined = active.And(highVal);

// Orders that are active OR high value
var either = active.Or(highVal);

var results = await context.Orders.FindAsync(combined);
```

The `And` / `Or` methods return a new `Specification<T>` that copies all configuration (includes, ordering, paging, etc.) from the left-hand specification and applies the merged criteria.

---

## Projecting Specifications

Use `Specification<T, TResult>` when you want the spec to project to a DTO as part of the query — avoiding loading full entities when you only need a subset of columns.

```csharp
public class OrderSummarySpec : Specification<Order, OrderSummaryDto>
{
    public OrderSummarySpec(int customerId)
    {
        AddCriteria(o => o.CustomerId == customerId);
        ApplyOrderByDescending(o => o.CreatedAt);
        ApplySelector(o => new OrderSummaryDto
        {
            Id    = o.Id,
            Total = o.Total,
            Date  = o.CreatedAt
        });
    }
}
```

```csharp
var summaries = await context.Orders.ToListAsync(new OrderSummarySpec(customerId));
// Returns IReadOnlyList<OrderSummaryDto>
```

---

## Inline Builder

For one-off queries where creating a class is overkill:

```csharp
var spec = new SpecificationBuilder<Product>()
    .AddCriteria(p => p.IsActive)
    .AddInclude(p => p.Category)
    .ApplyOrderBy(p => p.Name)
    .ApplyPaging(skip: 0, take: 25)
    .ApplyAsNoTracking();

var products = await context.Products.FindAsync(spec);
```

`SpecificationBuilder<T>` has a fluent API — every method returns `this` — so you can chain all configuration in one expression.

---

## Paginating with a Specification

```csharp
var spec = new ActiveOrdersSpec(customerId); // no ApplyPaging set
var page = await context.Orders.ToPagedFromSpecAsync(spec, page: 2, pageSize: 25);
```

`ToPagedFromSpecAsync` applies the spec criteria/includes/ordering and then runs the offset pagination on top.
