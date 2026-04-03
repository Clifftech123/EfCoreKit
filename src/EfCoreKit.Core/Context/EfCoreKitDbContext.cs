using EfCoreKit.Abstractions.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EfCoreKit.Core.Context;

/// <summary>
/// Base <see cref="DbContext"/> that provides automatic soft delete, audit trail,
/// multi-tenancy, and bulk operation support.
/// </summary>
/// <typeparam name="TContext">The derived context type.</typeparam>
public abstract class EfCoreKitDbContext<TContext> : DbContext
    where TContext : DbContext
{
    private readonly EfCoreKitOptions _options;
    private readonly IUserProvider? _userProvider;
    private readonly ITenantProvider? _tenantProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreKitDbContext{TContext}"/> class.
    /// </summary>
    /// <param name="options">The EF Core context options.</param>
    /// <param name="kitOptions">The EfCoreKit configuration options.</param>
    protected EfCoreKitDbContext(
        DbContextOptions<TContext> options,
        EfCoreKitOptions kitOptions)
        : base(options)
    {
        _options = kitOptions ?? throw new ArgumentNullException(nameof(kitOptions));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreKitDbContext{TContext}"/> class
    /// with user and tenant provider support.
    /// </summary>
    /// <param name="options">The EF Core context options.</param>
    /// <param name="kitOptions">The EfCoreKit configuration options.</param>
    /// <param name="userProvider">The user provider for audit trail.</param>
    /// <param name="tenantProvider">The tenant provider for multi-tenancy.</param>
    protected EfCoreKitDbContext(
        DbContextOptions<TContext> options,
        EfCoreKitOptions kitOptions,
        IUserProvider? userProvider,
        ITenantProvider? tenantProvider)
        : base(options)
    {
        _options = kitOptions ?? throw new ArgumentNullException(nameof(kitOptions));
        _userProvider = userProvider;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Gets the current EfCoreKit configuration options.
    /// </summary>
    protected EfCoreKitOptions Options => _options;

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaveChanges();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureGlobalFilters(modelBuilder);
    }

    /// <summary>
    /// Hook called before changes are saved. Handles audit trail, soft delete, and tenant assignment.
    /// </summary>
    private void OnBeforeSaveChanges()
    {
        // TODO: Implement audit trail (HandleAuditableEntities)
        // TODO: Implement soft delete interception (HandleSoftDeletableEntities)
        // TODO: Implement tenant assignment (HandleTenantEntities)
    }

    /// <summary>
    /// Applies global query filters for soft delete and multi-tenancy.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    private void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        // TODO: Apply ISoftDeletable global filter
           
           

        // TODO: Apply ITenantEntity global filter
    }
}
