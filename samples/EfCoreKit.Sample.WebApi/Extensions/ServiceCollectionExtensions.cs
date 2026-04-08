using EfCoreKit.Sample.WebApi.Application.Interfaces;
using EfCoreKit.Sample.WebApi.Infrastructure.Providers;
using EfCoreKit.Sample.WebApi.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EfCoreKit.Sample.WebApi.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> that keep <c>Program.cs</c> clean
/// by grouping related service registrations behind descriptive method names.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers EfCoreKit (DbContext, repositories, interceptors) and ASP.NET Core Identity
    /// against the same <see cref="AppDbContext"/>.
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // EfCoreKit: DbContext, IRepository<T>, IReadRepository<T>, IUnitOfWork,
        //            HttpContextUserProvider, and all interceptors (Audit, SoftDelete, SlowQuery).
        services.AddEfCoreExtensions<AppDbContext>(
            options => options.UseSqlServer(connectionString),
            kit => kit
                .EnableSoftDelete()
                .EnableAuditTrail()
                .UseUserProvider<HttpContextUserProvider>()
                .LogSlowQueries(TimeSpan.FromSeconds(2))
        );

        // ASP.NET Core Identity — plugs onto the same AppDbContext.
        services
            .AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication (HMAC-SHA256) and adds authorization services.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    // Whitelist only HMAC-SHA256 — prevents algorithm substitution attacks
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Registers application-level scoped services.
    /// <see cref="ICurrentUserService"/> is the single source of truth for who is logged in —
    /// EfCoreKit's user provider delegates to it so audit stamps always agree.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
