# Dynamic Filters

`ApplyFilters` lets you apply a runtime array of filter descriptors to any `IQueryable<T>`. This is useful when filters come from an API request, a frontend grid component, or user-defined query parameters.

## Basic Usage

```csharp
var filters = new[]
{
    new FilterDescriptor("Status",    "eq",      "Active"),
    new FilterDescriptor("CreatedAt", "gte",     DateTime.UtcNow.AddDays(-30)),
    new FilterDescriptor("Name",      "contains","Pro"),
};

var results = await context.Products
    .ApplyFilters(filters)
    .ToListAsync();
```

Each `FilterDescriptor` targets a property by name (supports nested paths like `"Address.City"`) and applies the specified operator against a value.

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

---

## FilterDescriptor

```csharp
public class FilterDescriptor
{
    public string Field    { get; set; }  // property name or path (e.g. "Address.City")
    public string Operator { get; set; }  // one of the operators above
    public object? Value   { get; set; } // the value to compare against
}
```

---

## Examples by Operator

### Equality / Comparison

```csharp
new FilterDescriptor("Status",  "eq",  "Active")
new FilterDescriptor("Status",  "ne",  "Cancelled")
new FilterDescriptor("Price",   "gt",  100m)
new FilterDescriptor("Price",   "gte", 50m)
new FilterDescriptor("Score",   "lt",  10)
new FilterDescriptor("Score",   "lte", 9)
```

### String Operators

```csharp
new FilterDescriptor("Name",  "contains",   "widget")
new FilterDescriptor("Email", "startswith", "admin")
new FilterDescriptor("Code",  "endswith",   "-X")
```

### Null Checks

```csharp
new FilterDescriptor("DeletedAt", "isnull",    null)
new FilterDescriptor("UpdatedAt", "isnotnull", null)
```

### In — Match Any Value in a List

```csharp
new FilterDescriptor("Status", "in", new[] { "Active", "Pending" })
new FilterDescriptor("Id",     "in", new[] { 1, 2, 3, 4 })
```

The value can be any `IEnumerable`; EfCore.Extensions builds a `List.Contains` expression that translates to SQL `IN (...)`.

### Between — Range Filter

```csharp
new FilterDescriptor("Price",     "between", new object[] { 10.0m, 99.99m })
new FilterDescriptor("CreatedAt", "between", new object[] { startDate, endDate })
```

Produces `property >= min && property <= max`.

---

## Combining with Other Helpers

`ApplyFilters` returns `IQueryable<T>`, so you can chain it with ordering, pagination, and specifications:

```csharp
var filters = new[] { new FilterDescriptor("IsActive", "eq", true) };
var sorts   = new[] { new SortDescriptor("CreatedAt", ascending: false) };

var page = await context.Customers
    .ApplyFilters(filters)
    .ApplySorts(sorts)
    .ToPagedAsync(page: 1, pageSize: 20);
```
