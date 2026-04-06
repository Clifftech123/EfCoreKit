using EfCoreKit.Sample.WebApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Data.Configurations;

/// <summary>
/// Configures the Identity schema manually so we can keep EfCoreDbContext as our base
/// class (which wires up EfCoreKit's global query filters) without inheriting from
/// IdentityDbContext. AddEntityFrameworkStores&lt;AppDbContext&gt;() only requires DbContext.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("AspNetUsers");
        b.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
        b.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");
        b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();
        b.Property(u => u.UserName).HasMaxLength(256);
        b.Property(u => u.NormalizedUserName).HasMaxLength(256);
        b.Property(u => u.Email).HasMaxLength(256);
        b.Property(u => u.NormalizedEmail).HasMaxLength(256);
        b.Property(u => u.TenantId).HasMaxLength(100).IsRequired();

        // Identity internal join tables — managed by UserManager, never queried directly
        b.HasMany<IdentityUserClaim<Guid>>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
        b.HasMany<IdentityUserLogin<Guid>>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
        b.HasMany<IdentityUserToken<Guid>>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
        b.HasMany<IdentityUserRole<Guid>>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();

        // Domain navigation — posts and comments written by this user
        b.HasMany(u => u.Posts).WithOne(p => p.User).HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(u => u.Comments).WithOne(c => c.User).HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> b)
    {
        b.ToTable("AspNetRoles");
        b.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();
        b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();
        b.Property(r => r.Name).HasMaxLength(256);
        b.Property(r => r.NormalizedName).HasMaxLength(256);
        b.HasMany<IdentityUserRole<Guid>>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
        b.HasMany<IdentityRoleClaim<Guid>>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
    }
}

public sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> b)
        => b.ToTable("AspNetUserClaims");
}

public sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> b)
    {
        b.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });
        b.ToTable("AspNetUserLogins");
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> b)
    {
        b.HasKey(ur => new { ur.UserId, ur.RoleId });
        b.ToTable("AspNetUserRoles");
    }
}

public sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> b)
        => b.ToTable("AspNetRoleClaims");
}

public sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> b)
    {
        b.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });
        b.ToTable("AspNetUserTokens");
    }
}
