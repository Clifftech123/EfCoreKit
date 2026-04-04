# Specification Pattern

Specifications encapsulate query logic in strongly-typed, reusable classes — and compose them at runtime with `And` / `Or`.

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
        ApplyThenBy(o => o.Id);          // secondary sort — requires OrderBy/OrderByDescending first
        ApplyPaging(skip: 0, take: 20);
        ApplyAsNoTracking();
    }
}
```

## What You Can Configure

| Method | Applied as | Notes |
|--------|-----------|-------|
| `AddCriteria(expr)` | `.Where(expr)` | Multiple calls **replace** the previous criteria (last wins) |
| `AddInclude(expr)` | `.Include(expr)` | Additive |
| `AddInclude(string)` | `.Include("Navigation.Path")` | Additive |
| `ApplyOrderBy(expr)` | `.OrderBy(expr)` | Mutually exclusive with `OrderByDescending` |
| `ApplyOrderByDescending(expr)` | `.OrderByDescending(expr)` | Mutually exclusive with `OrderBy` |
| `ApplyThenBy(expr)` | `.ThenBy(expr)` | Must call after `ApplyOrderBy` / `ApplyOrderByDescending` |
| `ApplyThenByDescending(expr)` | `.ThenByDescending(expr)` | Must call after `ApplyOrderBy` / `ApplyOrderByDescending` |
| `ApplyPaging(skip, take)` | `.Skip(skip).Take(take)` | |
| `ApplyAsNoTracking()` | `.AsNoTracking()` | |
| `ApplyAsSplitQuery()` | `.AsSplitQuery()` | |

> **`AddCriteria` note:** calling `AddCriteria` twice replaces the first call — the last value wins. To combine multiple conditions, use a single expression: `AddCriteria(x => x.IsActive && x.Age > 18)`.

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

Combine two specifications at runtime. The merged spec has the criteria from both, plus all includes from both.

```csharp
var active  = new ActiveOrdersSpec(customerId);
var highVal = new HighValueOrdersSpec(minTotal: 500);

// Orders that are active AND high value
var combined = active.And(highVal);

// Orders that are active OR high value
var either = active.Or(highVal);

var results = await context.Orders.FindAsync(combined);
```

**What And/Or copies:**
- Criteria — merged with `&&` (And) or `||` (Or) as an expression tree
- All includes from both left and right specs are merged

**What And/Or does NOT copy:**
- Ordering, paging, `AsNoTracking`, `AsSplitQuery` — these are **not** inherited from either spec. Add them to the result if needed using `ApplySpecification` manually, or put them in one of the source specs and apply that spec directly after composing.

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
// Returns IReadOnlyList<OrderSummaryDto> — only the selector columns are fetched
var summaries = await context.Orders.ToListAsync(new OrderSummarySpec(customerId));
```

> **Important:** `ToListAsync(ISpecification<T, TResult>)` throws `InvalidOperationException` if the spec has no `Selector` defined.

---

## ISpecification Interface

If you need to work with specs in a generic way, the full interface is:

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>>?                              Criteria          { get; }
    List<Expression<Func<T, object>>>                       Includes          { get; }
    List<string>                                            IncludeStrings    { get; }
    Expression<Func<T, object>>?                            OrderBy           { get; }
    Expression<Func<T, object>>?                            OrderByDescending { get; }
    List<(Expression<Func<T, object>> KeySelector, bool Ascending)> ThenByExpressions { get; }
    int?  Take        { get; }
    int?  Skip        { get; }
    bool  AsNoTracking { get; }
    bool  AsSplitQuery { get; }
}

// Projecting variant
public interface ISpecification<T, TResult> : ISpecification<T>
{
    Expression<Func<T, TResult>>? Selector { get; }
}
```

---

## Inline Builder

For one-off queries where creating a class is overkill:

```csharp
var spec = new SpecificationBuilder<Product>()
    .AddCriteria(p => p.IsActive)
    .AddInclude(p => p.Category)
    .ApplyOrderBy(p => p.Name)
    .ApplyThenByDescending(p => p.Price)
    .ApplyPaging(skip: 0, take: 25)
    .ApplyAsNoTracking();

var products = await context.Products.FindAsync(spec);
```

Every `SpecificationBuilder<T>` method returns `this`, so all calls can be chained.

---

## Paginating with a Specification

```csharp
// Apply spec criteria/includes/ordering, then paginate on top
var page = await context.Orders.ToPagedFromSpecAsync(
    new ActiveOrdersSpec(customerId),  // note: do NOT call ApplyPaging on the spec when using this
    page:     2,
    pageSize: 25);
```

`ToPagedFromSpecAsync` ignores any `Skip`/`Take` set on the spec — pagination is controlled by the `page`/`pageSize` parameters.
