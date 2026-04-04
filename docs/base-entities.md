# Base Entities

EfCore.Extensions provides a hierarchy of ready-made base classes so you don't have to repeat the same property declarations across every entity. Pick the one that matches the features you need.

## Entity Hierarchy

```
BaseEntity<TKey>
└── AuditableEntity<TKey>      (+ CreatedAt/By, UpdatedAt/By)
    └── SoftDeletableEntity<TKey>   (+ IsDeleted, DeletedAt/By)
        └── FullEntity<TKey>        (+ TenantId, RowVersion)
```

Each level adds the interface properties for the corresponding feature. All levels have an `int`-key convenience alias (e.g. `BaseEntity` = `BaseEntity<int>`).

## The Base Classes

### BaseEntity&lt;TKey&gt; / BaseEntity

```csharp
public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;
}

// Shorthand for int PK
public abstract class BaseEntity : BaseEntity<int> { }
```

Use when you want a strongly-typed `Id` property and nothing else from the library.

```csharp
public class Tag : BaseEntity       // int Id
public class Product : BaseEntity<Guid>  // Guid Id
```

### AuditableEntity&lt;TKey&gt; / AuditableEntity

Implements `IAuditable`. Fields are automatically stamped by `AuditInterceptor`.

```csharp
public abstract class AuditableEntity<TKey> : BaseEntity<TKey>, IAuditable
{
    public DateTime  CreatedAt { get; set; }
    public string?   CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string?   UpdatedBy { get; set; }
}
```

```csharp
public class Article : AuditableEntity<Guid> { }
```

### SoftDeletableEntity&lt;TKey&gt; / SoftDeletableEntity

Implements `IAuditable` + `ISoftDeletable`. Deletions are converted to soft deletes by `SoftDeleteInterceptor`.

```csharp
public abstract class SoftDeletableEntity<TKey> : AuditableEntity<TKey>, ISoftDeletable
{
    public bool      IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string?   DeletedBy { get; set; }
}
```

```csharp
public class Customer : SoftDeletableEntity { }
```

### FullEntity&lt;TKey&gt; / FullEntity

Implements everything: `IAuditable`, `ISoftDeletable`, `ITenantEntity`, and `IConcurrencyAware`.

```csharp
public abstract class FullEntity<TKey> : SoftDeletableEntity<TKey>, ITenantEntity, IConcurrencyAware
{
    public string? TenantId    { get; set; }
    public byte[]  RowVersion  { get; set; } = [];
}
```

```csharp
public class Invoice : FullEntity { }
```

---

## Entity Configuration Bases

Use the configuration base classes to auto-apply standard EF Core mappings without writing them yourself.

### BaseEntityConfiguration&lt;TEntity, TKey&gt;

```csharp
public abstract class BaseEntityConfiguration<TEntity, TKey>
    : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity<TKey>
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
```

### AuditableEntityConfiguration&lt;TEntity, TKey&gt;

Adds on top of `BaseEntityConfiguration`:
- Audit columns configured (no extra code needed)
- Index on `CreatedAt`

### SoftDeletableEntityConfiguration&lt;TEntity, TKey&gt;

Adds on top of `AuditableEntityConfiguration`:
- `IsDeleted` default value of `false`
- Composite index on `(IsDeleted, CreatedAt)`

### Usage

```csharp
// Shorthand generic aliases for int PK entities
public class CustomerConfiguration : SoftDeletableEntityConfiguration<Customer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
```

Register your configurations in `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

---

## IFullAuditable — Field-Level Audit Log

Add `IFullAuditable` to any entity when you want every property change recorded in an `AuditLog` table:

```csharp
public class Invoice : AuditableEntity, IFullAuditable { }
```

`IFullAuditable` extends `IAuditable` — no extra properties on your entity. You need `fullLog: true` in your setup and a `DbSet<AuditLog>` on your context. See [Audit Trail](audit-trail.md) for details.

---

## IConcurrencyAware

Add `IConcurrencyAware` to use EF Core row-version optimistic concurrency:

```csharp
public class Order : AuditableEntity, IConcurrencyAware
{
    public byte[] RowVersion { get; set; } = [];
}
```

When a `DbUpdateConcurrencyException` occurs, `EfCoreDbContext` automatically wraps it in a `ConcurrencyConflictException`:

```csharp
try
{
    await context.SaveChangesAsync();
}
catch (ConcurrencyConflictException ex)
{
    // ex.EntityType — name of the conflicting entity
    // ex.EntityId   — primary key of the conflicting row
}
```
