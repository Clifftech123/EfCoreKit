using EfCore.Extensions.Abstractions.Interfaces;
using EfCore.Extensions.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Extensions.Core.UnitOfWork;

/// <summary>
/// Coordinates multiple <see cref="Repository{T}"/> instances that share a single
/// <see cref="DbContext"/>, committing or rolling back all changes as a unit.
/// </summary>
/// <typeparam name="TContext">The application DbContext type.</typeparam>
public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    /// <summary>Initialises a new <see cref="UnitOfWork{TContext}"/>.</summary>
    public UnitOfWork(TContext context) => _context = context;

    /// <inheritdoc />
    public IRepository<T> Repository<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var cached))
            return (IRepository<T>)cached;

        var repo = new Repository<T>(_context);
        _repositories[typeof(T)] = repo;
        return repo;
    }

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    /// <inheritdoc />
    public Task RollbackAsync(CancellationToken ct = default)
    {
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() => _context.Dispose();
}
