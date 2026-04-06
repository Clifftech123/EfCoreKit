using System.Text;
using EfCoreKit.Exceptions;
using EfCoreKit.Extensions;
using EfCoreKit.Sample.WebApi.Application.Interfaces;
using EfCoreKit.Sample.WebApi.Domain.Entities;
using EfCoreKit.Sample.WebApi.Infrastructure.Data;
using EfCoreKit.Sample.WebApi.Infrastructure.Providers;
using EfCoreKit.Sample.WebApi.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// ── EfCoreKit ─────────────────────────────────────────────────────────────────
// Registers: AppDbContext, IRepository<T>, IReadRepository<T>, IUnitOfWork,
//            HttpContextUserProvider, HttpContextTenantProvider,
//            and all interceptors (Audit, SoftDelete, Tenant, SlowQuery).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddEfCoreExtensions<AppDbContext>(
    options => options.UseSqlServer(connectionString),
    kit => kit
        .EnableSoftDelete()               // SoftDeleteInterceptor + global query filter
        .EnableAuditTrail()               // stamps CreatedAt/By, UpdatedAt/By automatically
        .EnableMultiTenancy()             // scopes Post queries to tenant_id from the JWT
        .UseUserProvider<HttpContextUserProvider>()
        .UseTenantProvider<HttpContextTenantProvider>()
        .LogSlowQueries(TimeSpan.FromSeconds(2))
);

// ── ASP.NET Core Identity ─────────────────────────────────────────────────────
// Plugs onto the same AppDbContext that EfCoreKit already registered.
// AddEntityFrameworkStores<T> only needs DbContext — not IdentityDbContext.
builder.Services
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

// ── JWT Bearer Authentication ─────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!));

builder.Services
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

builder.Services.AddAuthorization();

// ── Application services ──────────────────────────────────────────────────────
// ICurrentUserService is the single source of truth for who is logged in.
// Both EfCoreKit providers (IUserProvider, ITenantProvider) delegate to it —
// so audit stamps and tenant scoping always agree with what the app sees.
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Global exception mapping ──────────────────────────────────────────────────
app.UseExceptionHandler(err => err.Run(async ctx =>
{
    var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var ex = feature?.Error;

    (int status, string message) = ex switch
    {
        EntityNotFoundException e      => (StatusCodes.Status404NotFound,            e.Message),
        ConcurrencyConflictException e => (StatusCodes.Status409Conflict,             e.Message),
        DuplicateEntityException e     => (StatusCodes.Status409Conflict,             e.Message),
        TenantMismatchException e      => (StatusCodes.Status403Forbidden,            e.Message),
        InvalidFilterException e       => (StatusCodes.Status400BadRequest,           e.Message),
        UnauthorizedAccessException e  => (StatusCodes.Status401Unauthorized,         e.Message),
        InvalidOperationException e    => (StatusCodes.Status400BadRequest,           e.Message),
        _                              => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };

    ctx.Response.StatusCode = status;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(new { error = message });
}));

// ── Dev tooling ───────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "EfCoreKit Sample API";
        options.Theme = ScalarTheme.BluePlanet;
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        // Tell Scalar to send the Bearer token automatically in all secured requests
        options.AddServer(new ScalarServer("https://localhost:7000"));
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();   // must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
