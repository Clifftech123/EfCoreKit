namespace EfCoreKit.Interfaces;

/// <summary>
/// Coordinates multiple repository operations under a single database commit.
/// Inject <see cref="IUnitOfWork"/> when a use-case touches more than one aggregate
/// and must commit or roll back as a unit.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Returns the repository for entity type <typeparamref name="T"/>.
    /// Repositories returned by the same <see cref="IUnitOfWork"/> share one DbContext.
    /// </summary>
    IRepository<T> Repository<T>() where T : class;

    /// <summary>Persists all staged changes from every repository in this unit of work.</summary>
    Task<int> CommitAsync(CancellationToken ct = default);

    /// <summary>Discards all staged changes and detaches every tracked entity.</summary>
    Task RollbackAsync(CancellationToken ct = default);
}
