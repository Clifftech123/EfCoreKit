# Dynamic Filters

`ApplyFilters` lets you apply a runtime array of filter descriptors to any `IQueryable<T>`. This is useful when filters come from an API request, a frontend grid component, or user-defined query parameters.

## Basic Usage

```csharp
var filters = new[]
{
    new FilterDescriptor { Field = "Status",    Operator = "eq",      Value = "Active" },
    new FilterDescriptor { Field = "CreatedAt", Operator = "gte",     Value = DateTime.UtcNow.AddDays(-30) },
    new FilterDescriptor { Field = "Name",      Operator = "contains", Value = "Pro" },
};

var results = await context.Products
    .ApplyFilters(filters)
    .ToListAsync();
```

`FilterDescriptor` uses `required init` properties — use object initializer syntax, not positional arguments.

Each descriptor targets a property by name (supports dot-separated nested paths like `"Address.City"`) and applies the specified operator against the value.

---

## FilterDescriptor

```csharp
public sealed class FilterDescriptor
{
    public required string Field    { get; init; }  // property name or path (e.g. "Address.City")
    public required string Operator { get; init; }  // operator string (case-insensitive)
    public object?         Value    { get; init; }  // the value to compare against
}
```

---

## Supported Operators

| Operator | Description | Value type |
|----------|-------------|------------|
| `eq` | Equal | Any comparable |
| `ne` | Not equal | Any comparable |
| `gt` | Greater than | IComparable |
| `gte` | Greater than or equal | IComparable |
| `lt` | Less than | IComparable |
| `lte` | Less than or equal | IComparable |
| `contains` | String contains (case-sensitive) | `string` |
| `startswith` | String starts with | `string` |
| `endswith` | String ends with | `string` |
| `isnull` | Property is null | _(value ignored)_ |
| `isnotnull` | Property is not null | _(value ignored)_ |
| `in` | Value is in a list | `IEnumerable` |
| `between` | Value is within a range | `object[2]` — `[min, max]` |

Operator strings are **case-insensitive** (`"EQ"`, `"Eq"`, `"eq"` all work).

---

## Examples by Operator

### Equality / Comparison

```csharp
new FilterDescriptor { Field = "Status",  Operator = "eq",  Value = "Active" }
new FilterDescriptor { Field = "Status",  Operator = "ne",  Value = "Cancelled" }
new FilterDescriptor { Field = "Price",   Operator = "gt",  Value = 100m }
new FilterDescriptor { Field = "Price",   Operator = "gte", Value = 50m }
new FilterDescriptor { Field = "Score",   Operator = "lt",  Value = 10 }
new FilterDescriptor { Field = "Score",   Operator = "lte", Value = 9 }
```

### String Operators

```csharp
new FilterDescriptor { Field = "Name",  Operator = "contains",   Value = "widget" }
new FilterDescriptor { Field = "Email", Operator = "startswith", Value = "admin" }
new FilterDescriptor { Field = "Code",  Operator = "endswith",   Value = "-X" }
```

### Null Checks

```csharp
new FilterDescriptor { Field = "DeletedAt", Operator = "isnull",    Value = null }
new FilterDescriptor { Field = "UpdatedAt", Operator = "isnotnull", Value = null }
```

### In — Match Any Value in a List

```csharp
new FilterDescriptor { Field = "Status", Operator = "in", Value = new[] { "Active", "Pending" } }
new FilterDescriptor { Field = "Id",     Operator = "in", Value = new[] { 1, 2, 3, 4 } }
```

The value can be any `IEnumerable`. EfCoreKit builds a `List.Contains` expression that translates to SQL `IN (...)`.

### Between — Inclusive Range Filter

```csharp
new FilterDescriptor { Field = "Price",     Operator = "between", Value = new object[] { 10.0m, 99.99m } }
new FilterDescriptor { Field = "CreatedAt", Operator = "between", Value = new object[] { startDate, endDate } }
```

Produces `property >= min && property <= max` (both bounds inclusive).

---

## SortDescriptor

Use `ApplySorts` to apply runtime sorting alongside filters:

```csharp
public sealed class SortDescriptor
{
    public required string Field     { get; init; }       // property name or nested path
    public          bool   Ascending { get; init; } = true; // default ascending
}
```

```csharp
var sorts = new[]
{
    new SortDescriptor { Field = "Category", Ascending = true },
    new SortDescriptor { Field = "Price",    Ascending = false },
};

var sorted = context.Products.ApplySorts(sorts);
```

The first descriptor maps to `OrderBy`, subsequent descriptors to `ThenBy`.

---

## Error Handling

`ApplyFilters` throws `InvalidFilterException` (inherits `EfCoreException`) in these cases:

| Situation | Exception message |
|-----------|------------------|
| `Field` is null or whitespace | `"Filter field name cannot be null or empty."` |
| Unsupported operator string | `"Unsupported filter operator: 'xyz'."` |
| `in` value is not `IEnumerable` | `"'in' operator requires an IEnumerable value."` |
| `between` value is not `object[2]` | `"'between' operator requires a 2-element object[] value: [min, max]."` |

---

## Combining with Other Helpers

`ApplyFilters` and `ApplySorts` return `IQueryable<T>`, so you can chain them freely:

```csharp
var filters = new[] { new FilterDescriptor { Field = "IsActive", Operator = "eq", Value = true } };
var sorts   = new[] { new SortDescriptor   { Field = "CreatedAt", Ascending = false } };

var page = await context.Customers
    .ApplyFilters(filters)
    .ApplySorts(sorts)
    .ToPagedAsync(page: 1, pageSize: 20);
```
