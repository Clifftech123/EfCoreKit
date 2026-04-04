using EfCoreKit.Exceptions;
using EfCoreKit.Interfaces;
using EfCoreKit.Filters;
using EfCoreKit.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCoreKit.Context;

/// <summary>
/// Base <see cref="DbContext"/> that provides automatic soft delete, audit trail,
/// multi-tenancy, and bulk operation support via EF Core interceptors.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="DbContext"/> — all standard EF Core features remain
/// fully available. You can use LINQ queries, raw SQL (<c>Database.ExecuteSqlAsync</c>),
/// change tracking, migrations, <c>DbSet&lt;T&gt;</c>, and any EF Core provider feature
/// exactly as you would with a plain <see cref="DbContext"/>.
///
/// Save-pipeline behaviour (audit, soft delete, tenant enforcement) is handled by
/// dedicated <see cref="Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor"/>
/// instances registered in <see cref="OnConfiguring"/>. This means the same interceptors
/// can be used with any <see cref="DbContext"/> — inheriting from this class is optional.
/// </remarks>
/// <typeparam name="TContext">The derived context type.</typeparam>
public abstract class EfCoreDbContext<TContext> : DbContext
    where TContext : DbContext
{
    private readonly EfCoreOptions _options;
    private readonly IUserProvider? _userProvider;
    private readonly ITenantProvider? _tenantProvider;
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreDbContext{TContext}"/> class.
    /// </summary>
    /// <param name="options">The EF Core context options.</param>
    /// <param name="kitOptions">The EfCoreKit configuration options.</param>
    protected EfCoreDbContext(
        DbContextOptions<TContext> options,
        EfCoreOptions kitOptions)
        : base(options)
    {
        _options = kitOptions ?? throw new ArgumentNullException(nameof(kitOptions));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreDbContext{TContext}"/> class
    /// with user and tenant provider support.
    /// </summary>
    /// <param name="options">The EF Core context options.</param>
    /// <param name="kitOptions">The EfCoreKit configuration options.</param>
    /// <param name="userProvider">The user provider for audit trail.</param>
    /// <param name="tenantProvider">The tenant provider for multi-tenancy.</param>
    /// <param name="loggerFactory">Optional logger factory for slow query logging.</param>
    protected EfCoreDbContext(
        DbContextOptions<TContext> options,
        EfCoreOptions kitOptions,
        IUserProvider? userProvider,
        ITenantProvider? tenantProvider,
        ILoggerFactory? loggerFactory = null)
        : base(options)
    {
        _options = kitOptions ?? throw new ArgumentNullException(nameof(kitOptions));
        _userProvider = userProvider;
        _tenantProvider = tenantProvider;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Gets the current EfCoreKit configuration options.
    /// </summary>
    protected EfCoreOptions Options => _options;

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (_options.AuditTrailEnabled)
            optionsBuilder.AddInterceptors(new AuditInterceptor(_options, _userProvider));

        if (_options.SoftDeleteEnabled)
            optionsBuilder.AddInterceptors(new SoftDeleteInterceptor(_options, _userProvider));

        if (_options.MultiTenancyEnabled)
            optionsBuilder.AddInterceptors(new TenantInterceptor(_options, _tenantProvider));

        if (_options.SlowQueryThreshold is not null)
        {
            var logger = _loggerFactory?.CreateLogger<SlowQueryInterceptor>();
            optionsBuilder.AddInterceptors(new SlowQueryInterceptor(_options, logger));
        }

    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry      = ex.Entries.FirstOrDefault();
            var entityName = entry?.Entity.GetType().Name ?? "Unknown";
            var entityId   = entry?.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue;
            throw new ConcurrencyConflictException(entityName, entityId);
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureGlobalFilters(modelBuilder);
    }

    /// <summary>
    /// Applies global query filters for soft delete and multi-tenancy.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    private void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        if (_options.SoftDeleteEnabled)
            SoftDeleteQueryFilter.Apply(modelBuilder);

        if (_options.MultiTenancyEnabled)
            TenantQueryFilter.Apply(modelBuilder, _tenantProvider);
    }
}
