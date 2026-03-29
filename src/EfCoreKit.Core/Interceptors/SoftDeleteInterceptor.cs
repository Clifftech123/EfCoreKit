using EfCoreKit.Abstractions.Interfaces;
using EfCoreKit.Core.Context;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreKit.Core.Interceptors;

/// <summary>
/// Interceptor that converts physical deletes to soft deletes for entities
/// implementing <see cref="ISoftDeletable"/>.
/// </summary>
internal sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly EfCoreKitOptions _options;
    private readonly IUserProvider? _userProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteInterceptor"/> class.
    /// </summary>
    /// <param name="options">The EfCoreKit configuration options.</param>
    /// <param name="userProvider">The user provider for resolving who performed the delete.</param>
    public SoftDeleteInterceptor(EfCoreKitOptions options, IUserProvider? userProvider = null)
    {
        _options = options;
        _userProvider = userProvider;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // TODO: Iterate ChangeTracker entries implementing ISoftDeletable
        // - For Deleted state: change to Modified, set IsDeleted=true, DeletedAt, DeletedBy
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
