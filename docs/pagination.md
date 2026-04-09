# Pagination

EfCoreKit provides two pagination strategies: **offset-based** (traditional page numbers) and **keyset/cursor-based** (for high-volume data and infinite scroll).

## Offset Pagination

Best for: admin dashboards, search results with page numbers, moderate data volumes.

### ToPagedAsync

```csharp
var page = await context.Orders
    .Where(o => o.Status == OrderStatus.Active)
    .OrderBy(o => o.CreatedAt)
    .ToPagedAsync(page: 2, pageSize: 25);

page.Items           // Items on this page
page.TotalCount      // Total matching rows across all pages
page.TotalPages      // Calculated total page count
page.Page            // Current page number
page.PageSize        // Items per page
page.HasNextPage     // Whether there's a next page
page.HasPreviousPage // Whether there's a previous page
page.From            // 1-based index of the first item on this page
page.To              // 1-based index of the last item on this page
page.IsEmpty         // True when TotalCount == 0
```

### PagedResult&lt;T&gt; Model

```csharp
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }
    public bool IsEmpty { get; }
    public int From { get; }   // 1-based first item index
    public int To { get; }     // 1-based last item index

    public PagedResult<TDestination> Map<TDestination>(Func<T, TDestination> mapper);
}
```

### Mapping to DTOs

```csharp
var page = await context.Customers
    .Where(c => c.IsActive)
    .ToPagedAsync(page: 1, pageSize: 20);

var dtoPage = page.Map(c => new CustomerDto
{
    Name  = c.Name,
    Email = c.Email
});
```

### SelectToPagedAsync (Projection Before Materialization)

Project _before_ the query executes — only the columns you need are fetched:

```csharp
var page = await context.Customers
    .Where(c => c.IsActive)
    .SelectToPagedAsync(
        c => new CustomerDto { Name = c.Name, Email = c.Email },
        page: 1,
        pageSize: 20);
```

### Paginating from a Specification

```csharp
var spec = new ActiveOrdersSpec(customerId); // ApplyPaging already set on the spec
var page = await context.Orders.ToPagedFromSpecAsync(spec, page: 1, pageSize: 20);
```

### Validation

- `page` must be ≥ 1
- `pageSize` must be between 1 and 1000
- Throws `ArgumentOutOfRangeException` for invalid values

---

## Keyset (Cursor) Pagination

Best for: infinite scroll, real-time feeds, large datasets. More efficient than offset pagination because it does not scan skipped rows.

### ToKeysetPagedAsync

```csharp
// First page — no cursor
var first = await context.Orders
    .OrderBy(o => o.Id)
    .ToKeysetPagedAsync(o => o.Id, cursor: null, pageSize: 50);

// Next page — pass the cursor from the previous result
var next = await context.Orders
    .OrderBy(o => o.Id)
    .ToKeysetPagedAsync(o => o.Id, cursor: int.Parse(first.NextCursor!), pageSize: 50);
```

### KeysetPagedResult&lt;T&gt; Model

```csharp
public sealed class KeysetPagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public string? NextCursor { get; }      // Pass this to get the next page
    public string? PreviousCursor { get; }  // The cursor used for this page
    public bool HasMore { get; }            // Whether more pages exist
}
```

### How It Works

1. If a cursor is provided, adds `WHERE key > cursor` to the query
2. Fetches `pageSize + 1` rows to determine if there are more pages
3. Returns the key of the last item as the `NextCursor`

---

## When to Use Which

| | Offset | Keyset |
|---|---|---|
| **Jump to page N** | Yes | No |
| **Performance on large tables** | Degrades with depth | Consistent |
| **Infinite scroll** | Possible | Ideal |
| **Stable with concurrent inserts** | Can skip / duplicate rows | Always consistent |
| **Requires ordered query** | Recommended | Required |

---

[← Specification Pattern](specifications.md) | [Dynamic Filters →](dynamic-filters.md)
