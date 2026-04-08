using EfCoreKit.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;

namespace EfCoreKit.Sample.WebApi.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplication"/> that configure the middleware pipeline.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps EfCoreKit and standard exceptions to appropriate HTTP status codes.
    /// </summary>
    public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(err => err.Run(async ctx =>
        {
            var feature = ctx.Features.Get<IExceptionHandlerFeature>();
            var ex = feature?.Error;

            (int status, string message) = ex switch
            {
                EntityNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
                ConcurrencyConflictException e => (StatusCodes.Status409Conflict, e.Message),
                DuplicateEntityException e => (StatusCodes.Status409Conflict, e.Message),
                InvalidFilterException e => (StatusCodes.Status400BadRequest, e.Message),
                UnauthorizedAccessException e => (StatusCodes.Status401Unauthorized, e.Message),
                InvalidOperationException e => (StatusCodes.Status400BadRequest, e.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { error = message });
        }));

        return app;
    }

    /// <summary>
    /// Enables development tooling: ensures the database exists, seeds sample data,
    /// and maps OpenAPI + Scalar API reference endpoints.
    /// </summary>
    public static async Task UseDevToolingAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Development only: drop & recreate so the schema always matches the model.
        // Production apps should use EF Core migrations instead.
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // Seed sample data (users, categories, tags, posts, comments)
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        await DatabaseSeeder.SeedAsync(db, userManager);

        app.MapOpenApi();
        app.MapScalarApiReference();
    }
}
