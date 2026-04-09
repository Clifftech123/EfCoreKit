# Repository & Unit of Work

EfCoreKit provides a generic `IRepository<T>` and an `IUnitOfWork` that are registered automatically when you call `AddEfCoreExtensions`.

## Registration

Both are registered automatically:

```csharp
builder.Services.AddEfCoreExtensions<AppDbContext>(
    options => options.UseSqlServer(connectionString));

// Registered automatically:
// IRepository<T>     → Repository<T>
// IReadRepository<T> → Repository<T>
// IUnitOfWork        → UnitOfWork<AppDbContext>
```

No additional registration needed.

---

## IReadRepository&lt;T&gt;

Read-only contract — no mutations, no `SaveChanges`.

```csharp
public interface IReadRepository<T> where T : class
{
    Task<T?>               GetByIdAsync(object id, CancellationToken ct = default);
    Task<T>                GetByIdOrThrowAsync(object id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<T?>               FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool>             ExistsAsync(object id, CancellationToken ct = default);                          // by key
    Task<bool>             ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default); // by predicate
    Task<int>              CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task<PagedResult<T>>   GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
}
```

Inject `IReadRepository<T>` in query-only services (CQRS read side, report services) to make the intent clear.

---

## IRepository&lt;T&gt;

Full read/write contract — extends `IReadRepository<T>`.

```csharp
public interface IRepository<T> : IReadRepository<T> where T : class
{
    Task     AddAsync(T entity, CancellationToken ct = default);
    Task     AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void     Update(T entity);
    void     UpdateRange(IEnumerable<T> entities);
    void     Remove(T entity);
    Task     RemoveByIdAsync(object id, CancellationToken ct = default);
    Task     RemoveRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default); // loads then deletes
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

---

## IUnitOfWork

Groups multiple repository operations into a single transaction.

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class;
    Task<int>      CommitAsync(CancellationToken ct = default);
    Task           RollbackAsync(CancellationToken ct = default);
}
```

`Repository<T>()` caches repository instances by type — calling it multiple times returns the same instance.

---

## Usage Examples

### Simple service with IRepository

```csharp
public class OrderService(IRepository<Order> orders)
{
    public async Task<Order> CreateAsync(CreateOrderDto dto, CancellationToken ct = default)
    {
        var order = new Order { CustomerId = dto.CustomerId, Total = dto.Total };
        await orders.AddAsync(order, ct);
        await orders.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order?> GetAsync(int id, CancellationToken ct = default)
        => await orders.GetByIdAsync(id, ct);
}
```

### Multi-repository transaction with IUnitOfWork

```csharp
public class CheckoutService(IUnitOfWork uow)
{
    public async Task CheckoutAsync(CheckoutDto dto, CancellationToken ct = default)
    {
        var orders    = uow.Repository<Order>();
        var inventory = uow.Repository<InventoryItem>();

        var order = new Order { CustomerId = dto.CustomerId };
        await orders.AddAsync(order, ct);

        foreach (var item in dto.Items)
        {
            var stock = await inventory.GetByIdOrThrowAsync(item.ProductId, ct);
            stock.Quantity -= item.Quantity;
            inventory.Update(stock);
        }

        await uow.CommitAsync(ct); // single SaveChangesAsync call
    }
}
```

### Read-only service

```csharp
public class ReportService(IReadRepository<Order> orders)
{
    public async Task<IReadOnlyList<Order>> GetRecentAsync(CancellationToken ct = default)
        => await orders.FindAsync(
            o => o.CreatedAt > DateTime.UtcNow.AddDays(-30), ct);
}
```

### Rollback on error

```csharp
try
{
    await uow.CommitAsync();
}
catch
{
    await uow.RollbackAsync(); // detaches all tracked entities
    throw;
}
```

---

## Specification Support

Both `IRepository<T>` and `IReadRepository<T>` accept specifications in `FindAsync`:

```csharp
var spec = new ActiveOrdersSpec(customerId);
var orders = await repo.FindAsync(spec, ct);
```

See [Specification Pattern](specifications.md) for details.

---

[← Audit Trail](audit-trail.md) | [Specification Pattern →](specifications.md)
