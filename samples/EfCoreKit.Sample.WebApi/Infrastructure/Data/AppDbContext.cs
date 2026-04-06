using EfCoreKit.Context;
using EfCoreKit.Entities;
using EfCoreKit.Interfaces;
using EfCoreKit.Sample.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Data;

/// <summary>
/// Single DbContext that combines EfCoreKit features and ASP.NET Core Identity.
///
/// Base class is <see cref="EfCoreDbContext{TContext}"/> (not IdentityDbContext) so that
/// EfCoreKit's global query filters for soft-delete and multi-tenancy are wired up via
/// <c>base.OnModelCreating</c>. Identity entity configuration is applied through the
/// standard <see cref="IEntityTypeConfiguration{T}"/> classes in the Configurations folder,
/// which <c>ApplyConfigurationsFromAssembly</c> picks up automatically.
/// </summary>
public class AppDbContext : EfCoreDbContext<AppDbContext>
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        EfCoreOptions kitOptions,
        IUserProvider? userProvider,
        ITenantProvider? tenantProvider,
        ILoggerFactory? loggerFactory = null)
        : base(options, kitOptions, userProvider, tenantProvider, loggerFactory)
    { }

    // ── Domain ─────────────────────────────────────────────────────────────────
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ── Identity ───────────────────────────────────────────────────────────────
    // Only Users is exposed — all other Identity tables are managed internally
    // by UserManager / RoleManager through the EF Core stores.
    public DbSet<User> Users => Set<User>();

    // Interceptors are registered by AddEfCoreExtensions via DI.
    // Overriding here (empty) prevents EfCoreDbContext.OnConfiguring from adding
    // them a second time.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Applies EfCoreKit's soft-delete and tenant global query filters
        base.OnModelCreating(modelBuilder);

        // Applies all IEntityTypeConfiguration<T> classes in this assembly
        // (domain entities + Identity tables)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
