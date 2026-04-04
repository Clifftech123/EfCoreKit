using System.Linq.Expressions;
using EfCoreKit.Abstractions.Interfaces;
using EfCoreKit.Abstractions.Models;
using EfCoreKit.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Repositories;

/// <summary>
/// Default generic repository implementation backed by EF Core.
/// Registered automatically when you call <c>AddEfCoreExtensions(...).AddRepositories()</c>.
/// Can also be registered manually: <c>services.AddScoped(typeof(IRepository&lt;&gt;), typeof(Repository&lt;&gt;))</c>.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    /// <summary>The underlying DbContext shared within the current scope.</summary>
    protected readonly DbContext Context;

    /// <summary>The DbSet for <typeparamref name="T"/>.</summary>
    protected readonly DbSet<T> DbSet;

    /// <summary>
    /// Initialises a new <see cref="Repository{T}"/>.
    /// </summary>
    public Repository(DbContext context)
    {
        Context = context;
        DbSet   = context.Set<T>();
    }

    // ── Reads ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<T?> GetByIdAsync(object id, CancellationToken ct = default)
        => DbSet.GetByIdAsync(id, ct);

    /// <inheritdoc />
    public Task<T> GetByIdOrThrowAsync(object id, CancellationToken ct = default)
        => DbSet.GetByIdOrThrowAsync(id, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => DbSet.GetAllAsync(ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSetExtensions.FindAsync(DbSet, predicate, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> FindAsync(ISpecification<T> specification, CancellationToken ct = default)
        => await DbSet.ApplySpecification(specification).ToListAsync(ct);

    /// <inheritdoc />
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(predicate, ct);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(object id, CancellationToken ct = default)
        => DbSet.ExistsAsync(id, ct);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSet.ExistsAsync(predicate, ct);

    /// <inheritdoc />
    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => DbSet.CountAsync(predicate, ct);

    /// <inheritdoc />
    public Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        => DbSet.AsQueryable().ToPagedAsync(page, pageSize, ct);

    // ── Writes ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    /// <inheritdoc />
    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => DbSet.AddRangeAsync(entities, ct);

    /// <inheritdoc />
    public void Update(T entity) => DbSet.Update(entity);

    /// <inheritdoc />
    public void UpdateRange(IEnumerable<T> entities) => DbSet.UpdateRange(entities);

    /// <inheritdoc />
    public void Remove(T entity) => DbSet.Remove(entity);

    /// <inheritdoc />
    public async Task RemoveByIdAsync(object id, CancellationToken ct = default)
    {
        var entity = await GetByIdOrThrowAsync(id, ct);
        DbSet.Remove(entity);
    }

    /// <inheritdoc />
    public Task RemoveRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSet.RemoveRangeAsync(predicate, ct);

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
