using EfCoreKit.Abstractions.Interfaces;
using EfCoreKit.Core.Context;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreKit.Core.Interceptors;

/// <summary>
/// Interceptor that automatically sets <see cref="IAuditable"/> properties
/// (<c>CreatedAt</c>, <c>CreatedBy</c>, <c>UpdatedAt</c>, <c>UpdatedBy</c>)
/// on save.
/// </summary>
internal sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly EfCoreKitOptions _options;
    private readonly IUserProvider? _userProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditInterceptor"/> class.
    /// </summary>
    /// <param name="options">The EfCoreKit configuration options.</param>
    /// <param name="userProvider">The user provider for resolving the current user.</param>
    public AuditInterceptor(EfCoreKitOptions options, IUserProvider? userProvider = null)
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
        // TODO: Iterate ChangeTracker entries implementing IAuditable
        // - For Added: set CreatedAt, CreatedBy
        // - For Modified: set UpdatedAt, UpdatedBy; protect CreatedAt/CreatedBy
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
