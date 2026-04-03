# Bulk Operations

EfCoreKit provides high-performance batch operations that execute in a single database round trip instead of one command per row.

## Supported Operations

| Method | Description |
|--------|-------------|
| `BulkInsertAsync<T>` | Insert thousands of rows in one call |
| `BulkUpdateAsync<T>` | Update many rows by primary key |
| `BulkDeleteAsync<T>` | Delete many rows by primary key |
| `BulkUpsertAsync<T>` | Insert or update (merge) based on key match |

## Supported Databases

Each database has its own provider package with an optimized implementation:

| Package | Database | Registration |
|---------|----------|-------------|
| `EfCoreKit.SqlServer` | SQL Server 2016+ | `services.AddEfCoreKitSqlServer()` |
| `EfCoreKit.PostgreSql` | PostgreSQL 12+ | `services.AddEfCoreKitPostgreSql()` |
| `EfCoreKit.MySql` | MySQL 8.0+ | `services.AddEfCoreKitMySql()` |
| `EfCoreKit.Sqlite` | SQLite 3.x | `services.AddEfCoreKitSqlite()` |

Or install `EfCoreKit` (umbrella package) to get all providers at once.

## Setup

```csharp
builder.Services.AddEfCoreKit<AppDbContext>(
    options => options.UseSqlServer(connectionString));

// Register the bulk operations provider
builder.Services.AddEfCoreKitSqlServer();
```

## Usage

Inject `IBulkExecutor` and use it with your `DbContext`:

```csharp
public class OrderService
{
    private readonly AppDbContext _context;
    private readonly IBulkExecutor _bulk;

    public OrderService(AppDbContext context, IBulkExecutor bulk)
    {
        _context = context;
        _bulk = bulk;
    }

    public async Task ImportOrders(List<Order> orders)
    {
        await _bulk.BulkInsertAsync(_context, orders);
    }
}
```

### Insert

```csharp
var customers = Enumerable.Range(1, 10_000)
    .Select(i => new Customer { Name = $"Customer {i}" })
    .ToList();

await bulk.BulkInsertAsync(context, customers);
```

### Update

```csharp
// Load entities, modify them, then bulk update
var products = await context.Products.Where(p => p.Category == "Sale").ToListAsync();
foreach (var p in products) p.Price *= 0.9m; // 10% off

await bulk.BulkUpdateAsync(context, products);
```

### Delete

```csharp
var expired = await context.Orders.Where(o => o.ExpiresAt < DateTime.UtcNow).ToListAsync();
await bulk.BulkDeleteAsync(context, expired);
```

### Upsert (Insert or Update)

```csharp
// Inserts new rows, updates existing ones (matched by primary key)
await bulk.BulkUpsertAsync(context, incomingProducts);
```

## BulkConfig Options

All operations accept an optional `BulkConfig` for fine-tuning:

```csharp
await bulk.BulkInsertAsync(context, customers, new BulkConfig
{
    BatchSize = 5000,              // Rows per batch (default: 1000)
    Timeout = 60,                  // Seconds (default: 30)
    PreserveInsertOrder = true,    // Maintain list order (default: true)
    SetOutputIdentity = true,      // Populate generated IDs after insert
    UseTransaction = true,         // Wrap in transaction (default: true)
    TrackEntities = false,         // Add to EF change tracker after operation

    // Column control
    PropertiesToInclude = ["Name", "Email"],     // Only update these columns
    PropertiesToExclude = ["CreatedAt"],          // Skip these columns

    // Upsert key
    UpdateByProperties = ["ExternalId"],          // Match on this instead of PK

    // Progress reporting
    OnProgress = (processed, total) =>
        Console.WriteLine($"{processed}/{total}")
});
```

### BulkConfig Properties

| Property | Default | Description |
|----------|---------|-------------|
| `BatchSize` | 1000 | Number of rows per batch |
| `Timeout` | 30 | Command timeout in seconds |
| `PreserveInsertOrder` | `true` | Maintain the order of the input list |
| `SetOutputIdentity` | `false` | Populate auto-generated keys after insert |
| `UpdateByProperties` | `null` | Columns to match on for upsert (defaults to PK) |
| `PropertiesToInclude` | `null` | Only include these columns |
| `PropertiesToExclude` | `null` | Exclude these columns |
| `UseTransaction` | `true` | Wrap operation in a transaction |
| `TrackEntities` | `false` | Add entities to the change tracker after the operation |
| `OnProgress` | `null` | Callback for progress reporting `(processed, total)` |

## Performance Notes

- Bulk operations bypass the EF Core change tracker — they go directly to the database
- `BulkInsertAsync` with 10,000 rows is typically **10-50x faster** than `AddRange` + `SaveChanges`
- Set `BatchSize` based on your row size — larger rows benefit from smaller batches
- `SetOutputIdentity = true` adds overhead (an extra round trip) but populates generated keys
- Interceptors (audit, soft delete) do **not** apply to bulk operations since they bypass `SaveChanges`

