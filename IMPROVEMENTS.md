# EfCoreKit — What's Missing & What to Add

## Summary

After reading every source file, here is a complete breakdown of gaps — ordered from highest to lowest value to content developers.

---

## 1. BASE ENTITY CLASSES (HIGHEST VALUE)

**The problem:** Right now developers have to implement `IAuditable`, `ISoftDeletable`, `ITenantEntity`, and `IConcurrencyAware` themselves on every entity. That defeats the whole point of the library.

**What to add:** Ready-made base classes they just inherit.

```
src/EfCoreKit.Abstractions/Entities/BaseEntity.cs
src/EfCoreKit.Abstractions/Entities/AuditableEntity.cs
src/EfCoreKit.Abstractions/Entities/SoftDeletableEntity.cs
src/EfCoreKit.Abstractions/Entities/FullEntity.cs
```

### `BaseEntity.cs`

```csharp
namespace EfCoreKit.Abstractions.Entities;

/// <summary>
/// Minimal base class — just a typed primary key.
/// Use when you don't need audit or soft-delete.
/// </summary>
public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;
}

/// <summary>
/// Convenience alias with int key.
/// </summary>
public abstract class BaseEntity : BaseEntity<int> { }
```

### `AuditableEntity.cs`

```csharp
using EfCoreKit.Abstractions.Interfaces;

namespace EfCoreKit.Abstractions.Entities;

/// <summary>
/// Base class with audit fields automatically set by EfCoreKit's AuditInterceptor.
/// Inherit this to get CreatedAt, CreatedBy, UpdatedAt, UpdatedBy for free.
/// </summary>
public abstract class AuditableEntity<TKey> : BaseEntity<TKey>, IAuditable
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public abstract class AuditableEntity : AuditableEntity<int> { }
```

### `SoftDeletableEntity.cs`

```csharp
using EfCoreKit.Abstractions.Interfaces;

namespace EfCoreKit.Abstractions.Entities;

/// <summary>
/// Base class with audit + soft-delete fields.
/// Inherit this to get IsDeleted/DeletedAt/DeletedBy automatically handled.
/// </summary>
public abstract class SoftDeletableEntity<TKey> : AuditableEntity<TKey>, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public abstract class SoftDeletableEntity : SoftDeletableEntity<int> { }
```

### `FullEntity.cs`

```csharp
using EfCoreKit.Abstractions.Interfaces;

namespace EfCoreKit.Abstractions.Entities;

/// <summary>
/// Kitchen-sink base class: audit + soft-delete + multi-tenancy + concurrency.
/// Use when you want everything wired up with zero interface implementations.
/// </summary>
public abstract class FullEntity<TKey> : SoftDeletableEntity<TKey>, ITenantEntity, IConcurrencyAware
{
    public string? TenantId { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Automatically incremented by the database on each update.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}

public abstract class FullEntity : FullEntity<int> { }
```

**Usage after adding these:**
```csharp
// Before (what devs currently have to do):
public class Order : IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    // ... actual properties
}

// After (what devs do with base classes):
public class Order : SoftDeletableEntity
{
    public string CustomerName { get; set; } = "";
    public decimal Total { get; set; }
}
```

---

## 2. ENTITY CONFIGURATION BASE CLASS

**The problem:** Developers also have to manually configure `RowVersion`, index `CreatedAt`, `IsDeleted`, etc. on every entity.

**What to add:**

```
src/EfCoreKit.Core/Configuration/BaseEntityConfiguration.cs
src/EfCoreKit.Core/Configuration/AuditableEntityConfiguration.cs
src/EfCoreKit.Core/Configuration/SoftDeletableEntityConfiguration.cs
```

### `BaseEntityConfiguration.cs`

```csharp
using EfCoreKit.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreKit.Core.Configuration;

/// <summary>
/// EF Core entity configuration base for <see cref="BaseEntity{TKey}"/>.
/// Inherit instead of implementing IEntityTypeConfiguration directly.
/// </summary>
public abstract class BaseEntityConfiguration<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity<TKey>
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
```

### `AuditableEntityConfiguration.cs`

```csharp
using EfCoreKit.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreKit.Core.Configuration;

public abstract class AuditableEntityConfiguration<TEntity, TKey> : BaseEntityConfiguration<TEntity, TKey>
    where TEntity : AuditableEntity<TKey>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(256);
        builder.Property(e => e.UpdatedBy).HasMaxLength(256);

        // Index CreatedAt — common in audit queries and sorting
        builder.HasIndex(e => e.CreatedAt);

        ConfigureAuditableEntity(builder);
    }

    protected virtual void ConfigureAuditableEntity(EntityTypeBuilder<TEntity> builder) { }
}
```

### `SoftDeletableEntityConfiguration.cs`

```csharp
using EfCoreKit.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreKit.Core.Configuration;

public abstract class SoftDeletableEntityConfiguration<TEntity, TKey> : AuditableEntityConfiguration<TEntity, TKey>
    where TEntity : SoftDeletableEntity<TKey>
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.DeletedBy).HasMaxLength(256);

        // Composite index — nearly every soft-delete query filters both IsDeleted and another column
        builder.HasIndex(e => new { e.IsDeleted, e.CreatedAt });

        ConfigureSoftDeletableEntity(builder);
    }

    protected virtual void ConfigureSoftDeletableEntity(EntityTypeBuilder<TEntity> builder) { }
}
```

**Usage:**
```csharp
public class OrderConfiguration : SoftDeletableEntityConfiguration<Order, int>
{
    protected override void ConfigureSoftDeletableEntity(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Total).HasPrecision(18, 2);
    }
}
```

---

## 3. SOFT DELETE — RESTORE AND INCLUDE-DELETED EXTENSIONS

**The problem:** Once something is soft-deleted, there is no way to restore it or even query it through EfCoreKit. Developers have to call `IgnoreQueryFilters()` and write their own restore logic.

**What to add:** New extensions in `DbSetExtensions.cs`.

```csharp
// ─── Soft-delete helpers ──────────────────────────────────────────

/// <summary>
/// Returns the soft-deleted entity with the given key, or null.
/// Bypasses the global soft-delete filter.
/// </summary>
/// <example>
/// <code>
/// Order? deleted = await context.Orders.FindDeletedAsync(orderId);
/// </code>
/// </example>
public static async Task<T?> FindDeletedAsync<T>(
    this DbSet<T> dbSet,
    object id,
    CancellationToken cancellationToken = default)
    where T : class, ISoftDeletable
{
    return await dbSet
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(e => ((ISoftDeletable)e).IsDeleted && EF.Property<object>(e, "Id").Equals(id),
            cancellationToken);
}

/// <summary>
/// Returns all soft-deleted entities.
/// </summary>
/// <example>
/// <code>
/// var deletedOrders = await context.Orders.GetDeletedAsync();
/// </code>
/// </example>
public static async Task<IReadOnlyList<T>> GetDeletedAsync<T>(
    this DbSet<T> dbSet,
    CancellationToken cancellationToken = default)
    where T : class, ISoftDeletable
{
    return await dbSet
        .IgnoreQueryFilters()
        .Where(e => e.IsDeleted)
        .ToListAsync(cancellationToken);
}

/// <summary>
/// Restores a soft-deleted entity by setting IsDeleted = false.
/// Call SaveChangesAsync after this.
/// </summary>
/// <example>
/// <code>
/// await context.Orders.RestoreAsync(order);
/// await context.SaveChangesAsync();
/// </code>
/// </example>
public static Task RestoreAsync<T>(
    this DbSet<T> dbSet,
    T entity,
    CancellationToken cancellationToken = default)
    where T : class, ISoftDeletable
{
    entity.IsDeleted = false;
    entity.DeletedAt = null;
    entity.DeletedBy = null;
    return Task.CompletedTask;
}

/// <summary>
/// Permanently deletes an entity even if soft-delete is enabled.
/// Use when you need GDPR hard erasure or similar.
/// </summary>
/// <example>
/// <code>
/// await context.Orders.HardDeleteAsync(order);
/// await context.SaveChangesAsync();
/// </code>
/// </example>
public static Task HardDeleteAsync<T>(
    this DbSet<T> dbSet,
    T entity,
    CancellationToken cancellationToken = default)
    where T : class, ISoftDeletable
{
    dbSet.Remove(entity);
    return Task.CompletedTask;
}
```

**And a QueryableExtensions helper:**

```csharp
/// <summary>
/// Includes soft-deleted entities in the query results.
/// Use when you need to show deleted records (e.g. audit history, recycle bin).
/// </summary>
/// <example>
/// <code>
/// var allOrders = await context.Orders
///     .IncludeDeleted()
///     .ToListAsync();
/// </code>
/// </example>
public static IQueryable<T> IncludeDeleted<T>(
    this IQueryable<T> query)
    where T : class, ISoftDeletable
{
    return query.IgnoreQueryFilters();
}

/// <summary>
/// Returns only soft-deleted entities.
/// </summary>
/// <example>
/// <code>
/// var deletedOnly = await context.Orders
///     .OnlyDeleted()
///     .ToListAsync();
/// </code>
/// </example>
public static IQueryable<T> OnlyDeleted<T>(
    this IQueryable<T> query)
    where T : class, ISoftDeletable
{
    return query.IgnoreQueryFilters().Where(e => e.IsDeleted);
}
```

---

## 4. FILTER DESCRIPTOR — MISSING OPERATORS

**The problem:** `ApplyFilters` currently supports `eq/ne/gt/gte/lt/lte/contains/startswith/endswith`. Missing:
- `isnull` / `isnotnull` — very common for nullable fields
- `in` — WHERE field IN (1, 2, 3) — extremely common in list pages
- `between` — date ranges and numeric ranges

**What to add** — add these cases to the `switch` in `QueryableExtensions.ApplyFilters`:

```csharp
"isnull" => Expression.Equal(propertyAccess, Expression.Constant(null, propertyType)),

"isnotnull" => Expression.NotEqual(propertyAccess, Expression.Constant(null, propertyType)),

"in" => BuildInExpression(propertyAccess, filter.Value, propertyType),

"between" => BuildBetweenExpression(propertyAccess, filter.Value, propertyType),
```

**Helper methods to add inside the class:**

```csharp
private static Expression BuildInExpression(Expression propertyAccess, object? value, Type propertyType)
{
    if (value is not System.Collections.IEnumerable enumerable)
        throw new InvalidFilterException("'in' operator requires an IEnumerable value.");

    var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
    var values = enumerable.Cast<object>()
        .Select(v => Convert.ChangeType(v, underlyingType))
        .ToList();

    // Build: values.Contains(property)
    var listType = typeof(List<>).MakeGenericType(underlyingType);
    var list = Activator.CreateInstance(listType)!;
    var addMethod = listType.GetMethod("Add")!;
    foreach (var v in values) addMethod.Invoke(list, [v]);

    var containsMethod = listType.GetMethod("Contains", [underlyingType])!;
    return Expression.Call(Expression.Constant(list), containsMethod, propertyAccess);
}

private static Expression BuildBetweenExpression(Expression propertyAccess, object? value, Type propertyType)
{
    // Expects value to be an array/tuple of [min, max]
    if (value is not object[] range || range.Length != 2)
        throw new InvalidFilterException("'between' operator requires a 2-element array [min, max].");

    var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
    var min = Expression.Constant(Convert.ChangeType(range[0], underlyingType), propertyType);
    var max = Expression.Constant(Convert.ChangeType(range[1], underlyingType), propertyType);

    return Expression.AndAlso(
        Expression.GreaterThanOrEqual(propertyAccess, min),
        Expression.LessThanOrEqual(propertyAccess, max));
}
```

**Also add the `FilterDescriptor.Value` type description update** to make the API clear for users:

```csharp
// In FilterDescriptor.cs — update the Value property comment:

/// <summary>
/// The filter value.
/// <list type="bullet">
///   <item><description>For <c>eq/ne/gt/gte/lt/lte/contains/startswith/endswith</c>: a scalar value.</description></item>
///   <item><description>For <c>in</c>: an <c>IEnumerable</c> of values.</description></item>
///   <item><description>For <c>between</c>: a 2-element <c>object[]</c> as <c>[min, max]</c>.</description></item>
///   <item><description>For <c>isnull/isnotnull</c>: ignored (pass <c>null</c>).</description></item>
/// </list>
/// </summary>
public object? Value { get; set; }
```

---

## 5. SPECIFICATION PATTERN — MISSING FEATURES

### 5a. Multi-column ordering (ThenBy)

**The problem:** `Specification<T>` has `OrderBy` and `OrderByDescending` but only one of each. You can't do `ORDER BY Category ASC, Price DESC`.

**What to add** — new properties + protected method in `Specification<T>`:

```csharp
// Add to ISpecification<T> interface:
List<(Expression<Func<T, object>> KeySelector, bool Ascending)> ThenByExpressions { get; }

// Add to Specification<T>:
public List<(Expression<Func<T, object>> KeySelector, bool Ascending)> ThenByExpressions { get; } = [];

protected void ApplyThenBy(Expression<Func<T, object>> keySelector, bool ascending = true) =>
    ThenByExpressions.Add((keySelector, ascending));

protected void ApplyThenByDescending(Expression<Func<T, object>> keySelector) =>
    ThenByExpressions.Add((keySelector, false));
```

**And apply them in `QueryableExtensions.ApplySpecification`:**

```csharp
// After the existing OrderBy / OrderByDescending block, add:
if (specification.ThenByExpressions.Count > 0)
{
    var orderedQuery = query as IOrderedQueryable<T>;
    if (orderedQuery is null)
        throw new InvalidOperationException("ThenBy requires an OrderBy or OrderByDescending to be set first.");

    foreach (var (keySelector, ascending) in specification.ThenByExpressions)
    {
        orderedQuery = ascending
            ? orderedQuery.ThenBy(keySelector)
            : orderedQuery.ThenByDescending(keySelector);
    }
    query = orderedQuery;
}
```

### 5b. Specification combinators (And / Or / Not)

**The problem:** You can't compose two specifications together. Devs end up copy-pasting criteria.

**What to add:**

```
src/EfCoreKit.Core/Specifications/SpecificationExtensions.cs
```

```csharp
using System.Linq.Expressions;
using EfCoreKit.Abstractions.Interfaces;

namespace EfCoreKit.Core.Specifications;

public static class SpecificationExtensions
{
    /// <summary>
    /// Combines two specifications with a logical AND.
    /// </summary>
    /// <example>
    /// <code>
    /// var spec = new ActiveSpec().And(new VipSpec());
    /// </code>
    /// </example>
    public static ISpecification<T> And<T>(
        this ISpecification<T> left,
        ISpecification<T> right)
        where T : class
        => new CombinedSpecification<T>(left, right, CombineOperator.And);

    /// <summary>
    /// Combines two specifications with a logical OR.
    /// </summary>
    public static ISpecification<T> Or<T>(
        this ISpecification<T> left,
        ISpecification<T> right)
        where T : class
        => new CombinedSpecification<T>(left, right, CombineOperator.Or);
}

internal enum CombineOperator { And, Or }

internal sealed class CombinedSpecification<T> : Specification<T> where T : class
{
    public CombinedSpecification(ISpecification<T> left, ISpecification<T> right, CombineOperator op)
    {
        if (left.Criteria is not null && right.Criteria is not null)
        {
            var parameter = left.Criteria.Parameters[0];
            var rightBody = new ParameterReplacer(right.Criteria.Parameters[0], parameter)
                .Visit(right.Criteria.Body);

            var combined = op == CombineOperator.And
                ? Expression.AndAlso(left.Criteria.Body, rightBody)
                : Expression.OrElse(left.Criteria.Body, rightBody);

            AddCriteria(Expression.Lambda<Func<T, bool>>(combined, parameter));
        }
        else
        {
            if (left.Criteria is not null) AddCriteria(left.Criteria);
            if (right.Criteria is not null) AddCriteria(right.Criteria);
        }

        foreach (var inc in left.Includes) AddInclude(inc);
        foreach (var inc in right.Includes) AddInclude(inc);
        foreach (var inc in left.IncludeStrings) AddInclude(inc);
        foreach (var inc in right.IncludeStrings) AddInclude(inc);
    }
}

internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _from, _to;
    public ParameterReplacer(ParameterExpression from, ParameterExpression to)
        => (_from, _to) = (from, to);

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _from ? _to : base.VisitParameter(node);
}
```

### 5c. Projection specification `ISpecification<T, TResult>`

**The problem:** The current spec always returns `T`. You can't use a spec to project directly to a DTO.

**What to add:**

```csharp
// In ISpecification.cs — add a second interface:

/// <summary>
/// Specification that includes a projection selector.
/// Use when you want a spec to return a DTO instead of the entity.
/// </summary>
public interface ISpecification<T, TResult> : ISpecification<T> where T : class
{
    Expression<Func<T, TResult>>? Selector { get; }
}

// In Specification.cs — add a generic version:
public abstract class Specification<T, TResult> : Specification<T>, ISpecification<T, TResult>
    where T : class
{
    public Expression<Func<T, TResult>>? Selector { get; private set; }

    protected void ApplySelector(Expression<Func<T, TResult>> selector) =>
        Selector = selector;
}
```

**And a QueryableExtensions overload:**

```csharp
/// <summary>
/// Applies a projecting specification and returns a list of <typeparamref name="TResult"/>.
/// </summary>
public static async Task<List<TResult>> ToListAsync<T, TResult>(
    this IQueryable<T> query,
    ISpecification<T, TResult> specification,
    CancellationToken cancellationToken = default)
    where T : class
{
    var q = query.ApplySpecification(specification);
    if (specification.Selector is not null)
        return await q.Select(specification.Selector).ToListAsync(cancellationToken);
    throw new InvalidOperationException("Projecting specification has no Selector defined.");
}
```

---

## 6. AUDIT LOG (IFullAuditable is currently empty)

**The problem:** `IFullAuditable` extends `IAuditable` but adds nothing. There is no audit log table, no audit log entity, and the `AuditInterceptor` doesn't write change history anywhere. The interface is a stub.

**What to add:**

```
src/EfCoreKit.Abstractions/Entities/AuditLog.cs
src/EfCoreKit.Core/Interceptors/FullAuditInterceptor.cs
```

### `AuditLog.cs`

```csharp
namespace EfCoreKit.Abstractions.Entities;

/// <summary>
/// Represents one field-level change recorded for IFullAuditable entities.
/// Add a DbSet&lt;AuditLog&gt; to your DbContext to enable the audit log table.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>The entity type name (e.g. "Order").</summary>
    public string EntityType { get; set; } = "";

    /// <summary>The primary key value of the changed entity, serialized as string.</summary>
    public string EntityKey { get; set; } = "";

    /// <summary>The property that changed.</summary>
    public string PropertyName { get; set; } = "";

    /// <summary>The value before the change (null for new entities).</summary>
    public string? OldValue { get; set; }

    /// <summary>The value after the change (null for deletes).</summary>
    public string? NewValue { get; set; }

    /// <summary>Added, Modified, or Deleted.</summary>
    public string Action { get; set; } = "";

    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
}
```

### `FullAuditInterceptor.cs`

```csharp
using EfCoreKit.Abstractions.Entities;
using EfCoreKit.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreKit.Core.Interceptors;

/// <summary>
/// Intercepts SaveChanges and writes field-level change records to AuditLog
/// for all entities implementing IFullAuditable.
/// Requires a DbSet&lt;AuditLog&gt; on the DbContext.
/// </summary>
public class FullAuditInterceptor : SaveChangesInterceptor
{
    private readonly IUserProvider? _userProvider;

    public FullAuditInterceptor(IUserProvider? userProvider = null)
        => _userProvider = userProvider;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return result;
        WriteAuditLogs(eventData.Context);
        return result;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is null) return result;
        WriteAuditLogs(eventData.Context);
        return result;
    }

    private void WriteAuditLogs(DbContext context)
    {
        var now = DateTime.UtcNow;
        var user = _userProvider?.GetCurrentUser();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IFullAuditable &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType().Name;
            var keyValues = entry.Metadata.FindPrimaryKey()?.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                ?? [];
            var entityKey = string.Join(",", keyValues);
            var action = entry.State.ToString();

            var changedProperties = entry.State == EntityState.Added
                ? entry.Properties
                : entry.Properties.Where(p => p.IsModified);

            foreach (var prop in changedProperties)
            {
                var log = new AuditLog
                {
                    EntityType    = entityType,
                    EntityKey     = entityKey,
                    PropertyName  = prop.Metadata.Name,
                    OldValue      = entry.State == EntityState.Added ? null : prop.OriginalValue?.ToString(),
                    NewValue      = entry.State == EntityState.Deleted ? null : prop.CurrentValue?.ToString(),
                    Action        = action,
                    ChangedAt     = now,
                    ChangedBy     = user
                };

                context.Set<AuditLog>().Add(log);
            }
        }
    }
}
```

**How to enable it** — update `EfCoreKitOptions` and `ServiceCollectionExtensions` to register this interceptor when `IFullAuditable` entities exist:

```csharp
// In EfCoreKitOptions — add a new fluent method:

/// <summary>
/// Enables field-level audit logging for IFullAuditable entities.
/// Requires a DbSet&lt;AuditLog&gt; on your DbContext.
/// </summary>
public EfCoreKitOptions EnableFullAuditLog()
{
    FullAuditLogEnabled = true;
    return this;
}

public bool FullAuditLogEnabled { get; private set; }
```

---

## 7. GENERIC REPOSITORY PATTERN (OPTIONAL BUT COMMONLY NEEDED)

**The problem:** Many content developers expect a repository pattern. Right now EfCoreKit gives them extension methods on DbSet directly — which is fine — but a lot of devs coming from Clean Architecture expect `IRepository<T>`.

**What to add:**

```
src/EfCoreKit.Abstractions/Interfaces/IRepository.cs
src/EfCoreKit.Abstractions/Interfaces/IReadRepository.cs
src/EfCoreKit.Core/Repositories/Repository.cs
```

### `IReadRepository.cs`

```csharp
using EfCoreKit.Abstractions.Models;

namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Read-only repository contract. Use when a service only needs to query.
/// </summary>
public interface IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
    Task<T> GetByIdOrThrowAsync(object id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> ExistsAsync(object id, CancellationToken ct = default);
    Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Applies a specification and returns matching entities.</summary>
    Task<IReadOnlyList<T>> FindAsync(ISpecification<T> specification, CancellationToken ct = default);
}
```

### `IRepository.cs`

```csharp
namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Full CRUD repository contract.
/// </summary>
public interface IRepository<T> : IReadRepository<T> where T : class
{
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Remove(T entity);
    Task RemoveByIdAsync(object id, CancellationToken ct = default);
    Task RemoveRangeAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

### `Repository.cs`

```csharp
using System.Linq.Expressions;
using EfCoreKit.Abstractions.Exceptions;
using EfCoreKit.Abstractions.Interfaces;
using EfCoreKit.Abstractions.Models;
using EfCoreKit.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Repositories;

/// <summary>
/// Default generic repository implementation backed by EF Core DbContext.
/// Register with AddEfCoreKit or manually via DI.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly DbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(DbContext context)
    {
        Context = context;
        DbSet   = context.Set<T>();
    }

    public Task<T?> GetByIdAsync(object id, CancellationToken ct = default)
        => DbSet.GetByIdAsync(id, ct);

    public Task<T> GetByIdOrThrowAsync(object id, CancellationToken ct = default)
        => DbSet.GetByIdOrThrowAsync(id, ct);

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => DbSet.GetAllAsync(ct);

    public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSet.FindAsync(predicate, ct);

    public Task<bool> ExistsAsync(object id, CancellationToken ct = default)
        => DbSet.ExistsAsync(id, ct);

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => DbSet.CountAsync(predicate, ct);

    public Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        => DbSet.AsQueryable().ToPagedAsync(page, pageSize, ct);

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(predicate, ct);

    public async Task<IReadOnlyList<T>> FindAsync(ISpecification<T> specification, CancellationToken ct = default)
        => await DbSet.ApplySpecification(specification).ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => DbSet.AddRangeAsync(entities, ct);

    public void Update(T entity) => DbSet.Update(entity);

    public void UpdateRange(IEnumerable<T> entities) => DbSet.UpdateRange(entities);

    public void Remove(T entity) => DbSet.Remove(entity);

    public async Task RemoveByIdAsync(object id, CancellationToken ct = default)
    {
        var entity = await GetByIdOrThrowAsync(id, ct);
        DbSet.Remove(entity);
    }

    public Task RemoveRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSet.RemoveRangeAsync(predicate, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
```

**Registration helper** — add to `ServiceCollectionExtensions`:

```csharp
// Add as an optional override in AddEfCoreKit:

/// <summary>
/// Registers the generic repository so you can inject IRepository&lt;T&gt; directly.
/// </summary>
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
services.AddScoped(typeof(IReadRepository<>), typeof(Repository<>));
```

---

## 8. UNIT OF WORK

**The problem:** When you use multiple repositories in one request, each has its own `SaveChangesAsync`. You need a way to commit all changes in a single transaction.

**What to add:**

```
src/EfCoreKit.Abstractions/Interfaces/IUnitOfWork.cs
src/EfCoreKit.Core/UnitOfWork/UnitOfWork.cs
```

### `IUnitOfWork.cs`

```csharp
namespace EfCoreKit.Abstractions.Interfaces;

/// <summary>
/// Wraps multiple repository operations in a single commit.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
```

### `UnitOfWork.cs`

```csharp
using EfCoreKit.Abstractions.Interfaces;
using EfCoreKit.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.UnitOfWork;

public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(TContext context) => _context = context;

    public IRepository<T> Repository<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var existing))
            return (IRepository<T>)existing;

        var repo = new Repository<T>(_context);
        _repositories[typeof(T)] = repo;
        return repo;
    }

    public Task<int> CommitAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public Task RollbackAsync(CancellationToken ct = default)
    {
        foreach (var entry in _context.ChangeTracker.Entries())
            entry.State = EntityState.Detached;
        return Task.CompletedTask;
    }

    public void Dispose() => _context.Dispose();
}
```

---

## 9. DBCONTEXT UTILITY EXTENSIONS

**What to add** — new file `src/EfCoreKit.Core/Extensions/DbContextExtensions.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Extensions;

public static class DbContextExtensions
{
    /// <summary>
    /// Executes an action inside a database transaction.
    /// Commits on success, rolls back on any exception.
    /// </summary>
    /// <example>
    /// <code>
    /// await context.ExecuteInTransactionAsync(async () =>
    /// {
    ///     context.Orders.Add(order);
    ///     context.Inventory.Update(stock);
    ///     await context.SaveChangesAsync();
    /// });
    /// </code>
    /// </example>
    public static async Task ExecuteInTransactionAsync(
        this DbContext context,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Executes an action inside a database transaction and returns a result.
    /// </summary>
    public static async Task<T> ExecuteInTransactionAsync<T>(
        this DbContext context,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Detaches all tracked entities from the change tracker.
    /// Useful in long-running services or after bulk operations.
    /// </summary>
    public static void DetachAll(this DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;
    }

    /// <summary>
    /// Truncates all rows from the table — much faster than DeleteRange.
    /// WARNING: Does not respect soft-delete or foreign key constraints.
    /// </summary>
    public static async Task TruncateAsync<T>(
        this DbContext context,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var entityType = context.Model.FindEntityType(typeof(T))
            ?? throw new InvalidOperationException($"Entity type {typeof(T).Name} not found in model.");

        var tableName = entityType.GetTableName()!;
        var schema    = entityType.GetSchema();
        var fullName  = schema is null ? $"\"{tableName}\"" : $"\"{schema}\".\"{tableName}\"";

        await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {fullName}", cancellationToken);
    }
}
```

---

## 10. PAGERESULT — SMALL BUT USEFUL IMPROVEMENTS

**What to add** to `PagedResult<T>`:

```csharp
// Add these computed properties to PagedResult<T>:

/// <summary>True when there are no items on this page.</summary>
public bool IsEmpty => Items.Count == 0;

/// <summary>The 1-based index of the first item on this page (for "Showing X-Y of Z").</summary>
public int From => IsEmpty ? 0 : (Page - 1) * PageSize + 1;

/// <summary>The 1-based index of the last item on this page.</summary>
public int To => IsEmpty ? 0 : From + Items.Count - 1;
```

---

## 11. MISSING EXCEPTION: DuplicateEntityException

**The problem:** Unique constraint violations (email already taken, slug already used) throw a raw `DbUpdateException`. Developers have to catch that and parse the inner exception message. EfCoreKit should provide a clean `DuplicateEntityException`.

**What to add:**

```
src/EfCoreKit.Abstractions/Exceptions/DuplicateEntityException.cs
```

```csharp
namespace EfCoreKit.Abstractions.Exceptions;

/// <summary>
/// Thrown when an entity violates a unique constraint.
/// </summary>
public class DuplicateEntityException : EfCoreKitException
{
    public string EntityName { get; }
    public string? FieldName  { get; }
    public object? FieldValue { get; }

    public DuplicateEntityException(string entityName, string? fieldName = null, object? fieldValue = null)
        : base(BuildMessage(entityName, fieldName, fieldValue))
    {
        EntityName = entityName;
        FieldName  = fieldName;
        FieldValue = fieldValue;
    }

    private static string BuildMessage(string entityName, string? fieldName, object? fieldValue)
    {
        if (fieldName is null) return $"A {entityName} with the same unique key already exists.";
        return fieldValue is null
            ? $"A {entityName} with the same '{fieldName}' already exists."
            : $"A {entityName} with {fieldName} = '{fieldValue}' already exists.";
    }
}
```

---

## 12. IQUERYABLE HELPER: `AsQueryableOf<T>()` FOR SPECIFICATION RESULT

Currently `ApplySpecification` returns `IQueryable<T>`. If you want to then paginate, you call `ToPagedAsync`. But a common pattern is "apply spec, then paginate" — add a shortcut:

```csharp
// Add to QueryableExtensions:

/// <summary>
/// Applies a specification and returns a paged result in one call.
/// </summary>
/// <example>
/// <code>
/// var result = await context.Orders
///     .ToPagedFromSpecAsync(new RecentOrdersSpec(), page: 1, pageSize: 20);
/// </code>
/// </example>
public static async Task<PagedResult<T>> ToPagedFromSpecAsync<T>(
    this IQueryable<T> query,
    ISpecification<T> specification,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
    where T : class
{
    return await query
        .ApplySpecification(specification)
        .ToPagedAsync(page, pageSize, cancellationToken);
}
```

---

## 13. SLOW QUERY INTERCEPTOR — CONFIGURABLE LOG LEVEL

**The problem:** `SlowQueryInterceptor` always logs at `Warning` level. Teams want to control this — some want `Error` for really slow queries, `Information` for everything else.

**What to add** — update constructor to accept thresholds per log level:

```csharp
// Option 1 — simple, two thresholds:
public SlowQueryInterceptor(
    TimeSpan warningThreshold,
    TimeSpan? errorThreshold = null, // null = never log as Error
    ILogger<SlowQueryInterceptor>? logger = null)
{
    _warningThreshold = warningThreshold;
    _errorThreshold   = errorThreshold;
    _logger           = logger;
}

// In the log method:
private void LogIfSlow(string sql, TimeSpan duration)
{
    if (_errorThreshold.HasValue && duration >= _errorThreshold.Value)
    {
        _logger?.LogError("Slow query ({Duration}ms): {Sql}", duration.TotalMilliseconds, sql);
    }
    else if (duration >= _warningThreshold)
    {
        _logger?.LogWarning("Slow query ({Duration}ms): {Sql}", duration.TotalMilliseconds, sql);
    }
}
```

---

## 14. CONCURRENCY — THROW `ConcurrencyException` AUTOMATICALLY

**The problem:** `ConcurrencyException` exists in the project but is never thrown anywhere. The `IConcurrencyAware` interface exists but the `DbContext` doesn't catch `DbUpdateConcurrencyException` and wrap it.

**What to add** — override `SaveChangesAsync` in `EfCoreKitDbContext`:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    try
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException ex)
    {
        var entry = ex.Entries.FirstOrDefault();
        var entityName = entry?.Entity.GetType().Name ?? "Unknown";
        throw new ConcurrencyException(entityName, ex);
    }
}
```

---

## SUMMARY TABLE — PRIORITY ORDER

| # | What | Why it matters | Effort |
|---|------|---------------|--------|
| 1 | Base entity classes | Biggest boilerplate eliminator — most impactful thing you can add | Low |
| 2 | Entity configuration bases | Pairs with #1, eliminates config boilerplate | Low |
| 3 | Soft-delete restore extensions | Without it, devs can't restore deleted records via the library | Low |
| 4 | `IncludeDeleted` / `OnlyDeleted` | Devs need to see deleted records in admin pages | Low |
| 5 | Filter `isnull/isnotnull/in/between` | These operators come up constantly in real filter UIs | Medium |
| 6 | AuditLog entity + FullAuditInterceptor | Makes `IFullAuditable` actually do something | Medium |
| 7 | `IRepository<T>` + `Repository<T>` | Expected by clean-arch devs | Medium |
| 8 | `IUnitOfWork` | Needed when using multiple repos in one request | Low |
| 9 | `ExecuteInTransactionAsync` | Very common pattern — painful to write each time | Low |
| 10 | Spec `ThenBy` + combinators | Makes specs usable for real sorted, combined queries | Medium |
| 11 | `DuplicateEntityException` | Clean error handling for unique constraint violations | Low |
| 12 | `ConcurrencyException` auto-throw | Makes `IConcurrencyAware` actually do something | Low |
| 13 | `PagedResult.IsEmpty/From/To` | Polish — devs building "showing X-Y of Z" UI | Low |
| 14 | `ISpecification<T, TResult>` with projection | Advanced — needed for CQRS projections | Medium |
| 15 | `SlowQueryInterceptor` error threshold | Operational quality — nice but not critical | Low |
| 16 | `DetachAll()` / `TruncateAsync` | Useful for test cleanup and long-running services | Low |

---

## FILES TO CREATE OR MODIFY

### New files:
```
src/EfCoreKit.Abstractions/Entities/BaseEntity.cs
src/EfCoreKit.Abstractions/Entities/AuditableEntity.cs
src/EfCoreKit.Abstractions/Entities/SoftDeletableEntity.cs
src/EfCoreKit.Abstractions/Entities/FullEntity.cs
src/EfCoreKit.Abstractions/Entities/AuditLog.cs
src/EfCoreKit.Abstractions/Interfaces/IRepository.cs
src/EfCoreKit.Abstractions/Interfaces/IReadRepository.cs
src/EfCoreKit.Abstractions/Interfaces/IUnitOfWork.cs
src/EfCoreKit.Abstractions/Exceptions/DuplicateEntityException.cs
src/EfCoreKit.Core/Configuration/BaseEntityConfiguration.cs
src/EfCoreKit.Core/Configuration/AuditableEntityConfiguration.cs
src/EfCoreKit.Core/Configuration/SoftDeletableEntityConfiguration.cs
src/EfCoreKit.Core/Interceptors/FullAuditInterceptor.cs
src/EfCoreKit.Core/Repositories/Repository.cs
src/EfCoreKit.Core/Specifications/SpecificationExtensions.cs
src/EfCoreKit.Core/UnitOfWork/UnitOfWork.cs
src/EfCoreKit.Core/Extensions/DbContextExtensions.cs
```

### Files to modify:
```
src/EfCoreKit.Abstractions/Interfaces/IAuditable.cs         → IFullAuditable adds nothing, keep as marker for FullAuditInterceptor
src/EfCoreKit.Abstractions/Interfaces/ISpecification.cs     → add ThenByExpressions, and ISpecification<T,TResult>
src/EfCoreKit.Abstractions/Models/PagedResult.cs            → add IsEmpty, From, To
src/EfCoreKit.Core/Specifications/Specification.cs          → add ThenByExpressions support
src/EfCoreKit.Core/Extensions/QueryableExtensions.cs        → add IncludeDeleted, OnlyDeleted, isnull/in/between operators, ToPagedFromSpecAsync
src/EfCoreKit.Core/Extensions/DbSetExtensions.cs            → add FindDeletedAsync, GetDeletedAsync, RestoreAsync, HardDeleteAsync
src/EfCoreKit.Core/Context/EfCoreKitDbContext.cs            → override SaveChangesAsync to catch DbUpdateConcurrencyException
src/EfCoreKit.Core/Context/EfCoreKitDbContextOptions.cs     → add EnableFullAuditLog() fluent method
src/EfCoreKit.Core/Extensions/ServiceCollectionExtensions.cs → register Repository<T>, UnitOfWork
src/EfCoreKit.Core/Interceptors/SlowQueryInterceptor.cs     → add error threshold option
```
