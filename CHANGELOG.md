# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-04-09

### Added

#### Base Entity Hierarchy
- `BaseEntity<TKey>` / `BaseEntity` — strongly-typed primary key base class
- `AuditableEntity<TKey>` / `AuditableEntity` — adds `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- `SoftDeletableEntity<TKey>` / `SoftDeletableEntity` — adds `IsDeleted`, `DeletedAt`, `DeletedBy`
- `FullEntity<TKey>` / `FullEntity` — soft-delete + audit + optimistic concurrency (`RowVersion`)

#### Entity Configuration Bases
- `BaseEntityConfiguration<TEntity, TKey>` — auto-configures primary key
- `AuditableEntityConfiguration<TEntity, TKey>` — adds audit columns and `CreatedAt` index
- `SoftDeletableEntityConfiguration<TEntity, TKey>` — adds `IsDeleted` default and composite index

#### Interfaces
- `IAuditable` — audit timestamp contract
- `IFullAuditable` — field-level change history contract
- `ISoftDeletable` — soft delete contract
- `IConcurrencyAware` — optimistic concurrency contract
- `IUserProvider` — current user resolution contract
- `IRepository<T>` / `IReadRepository<T>` — repository contracts
- `IUnitOfWork` — unit of work contract
- `ISpecification<T>` / `ISpecification<T, TResult>` — specification pattern contracts

#### Soft Delete
- `SoftDeleteInterceptor` — intercepts `DELETE` and converts to `UPDATE SET IsDeleted = true`
- Global query filter automatically excludes soft-deleted rows
- `IncludeDeleted()` — bypass the filter to include soft-deleted rows
- `OnlyDeleted()` — query only soft-deleted rows
- `GetDeletedAsync()` — load all soft-deleted rows from a `DbSet<T>`
- `Restore()` — clear soft-delete flags on an entity
- `HardDelete()` — permanently remove a record bypassing the interceptor
- Optional cascade soft delete for loaded navigation properties

#### Audit Trail
- `AuditInterceptor` — auto-stamps `CreatedAt`/`CreatedBy` on insert and `UpdatedAt`/`UpdatedBy` on update
- Protects `CreatedAt` and `CreatedBy` from being overwritten on update
- Optional full audit log (`IFullAuditable`) — records every field change in an `AuditLog` table

#### Repository and Unit of Work
- `Repository<T>` — generic implementation of `IRepository<T>` and `IReadRepository<T>`
- `UnitOfWork<TContext>` — groups multiple repository operations into a single `SaveChangesAsync`
- All registered automatically via `AddEfCoreExtensions`

#### Specification Pattern
- `Specification<T>` base class with `AddCriteria`, `AddInclude`, `ApplyOrderBy`, `ApplyPaging`, `ApplyAsNoTracking`, `ApplyAsSplitQuery`
- `Specification<T, TResult>` — projecting specification with `ApplySelector`
- `And()` / `Or()` combinators — compose specifications at runtime
- `SpecificationBuilder<T>` — fluent inline builder for one-off queries
- `ApplySpecification` extension — apply any spec to an `IQueryable<T>`
- `ToListAsync(ISpecification<T, TResult>)` — project and materialise in one call
- `ToPagedFromSpecAsync` — apply spec criteria then paginate

#### Pagination
- `ToPagedAsync` — offset pagination returning `PagedResult<T>` with full metadata
- `SelectToPagedAsync` — project before materialising, then paginate
- `ToKeysetPagedAsync` — keyset/cursor pagination returning `KeysetPagedResult<T>`
- `PagedResult<T>.Map<TDestination>` — map items to a different type while preserving metadata

#### Dynamic Filters and Sorting
- `ApplyFilters(FilterDescriptor[])` — apply runtime filter arrays to any `IQueryable<T>`
- Supported operators: `eq`, `ne`, `gt`, `gte`, `lt`, `lte`, `contains`, `startswith`, `endswith`, `isnull`, `isnotnull`, `in`, `between`
- `ApplySorts(SortDescriptor[])` — apply runtime multi-column sorting
- Dot-separated nested property paths supported in both filters and sorts

#### Query Helpers
- `GetByIdAsync` / `GetByIdOrThrowAsync` — single entity lookup by primary key
- `GetByIdsAsync` — batch lookup translating to `WHERE key IN (...)`
- `GetAllAsync` — load all rows from a `DbSet<T>`
- `FindAsync` — load by predicate or specification
- `ExistsAsync` — existence check by key or predicate
- `WhereIf` / `WhereIfNotNull` / `WhereIfNotEmpty` — conditional filtering
- `OrderByDynamic` / `ThenByDynamic` — sort by property name string
- `SelectToListAsync` / `SelectFirstOrDefaultAsync` / `SelectDistinctAsync` — projection helpers
- `WithNoTracking` — alias for `AsNoTracking`
- `RemoveRangeAsync` — load and stage for deletion by predicate

#### DbContext Utilities
- `ExecuteInTransactionAsync` — wraps work in a transaction respecting EF Core execution strategy
- `DetachAll` — clears all tracked entities from the change tracker
- `TruncateAsync<T>` — truncates a table by entity type using EF Core metadata

#### Slow Query Logging
- `SlowQueryInterceptor` — logs a warning for any query exceeding a configurable threshold
- Configured via `LogSlowQueries(TimeSpan threshold)`

#### Structured Exceptions
- `EfCoreException` — abstract base for all EfCoreKit exceptions
- `EntityNotFoundException` — thrown by `GetByIdOrThrowAsync` and `RemoveByIdAsync`
- `ConcurrencyConflictException` — thrown automatically on stale row-version conflict
- `DuplicateEntityException` — throw manually when catching unique constraint violations
- `InvalidFilterException` — thrown by `ApplyFilters` for invalid filter descriptors

#### DI Registration
- `AddEfCoreExtensions<TContext>` — single call registers DbContext, interceptors, repository, and unit of work
- `EfCoreDbContext<TContext>` — optional base context that wires interceptors and query filters automatically
