using Microsoft.AspNetCore.Identity;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Data;

/// <summary>
/// Seeds the database with sample data so the API is immediately usable after first run.
/// Only runs when the database is empty (idempotent).
/// </summary>
public static class DatabaseSeeder
{
    // Deterministic IDs for cross-entity references
    private static readonly Guid AliceId = Guid.Parse("a1a1a1a1-b2b2-c3c3-d4d4-e5e5e5e5e5e5");
    private static readonly Guid BobId = Guid.Parse("b1b1b1b1-c2c2-d3d3-e4e4-f5f5f5f5f5f5");

    public static async Task SeedAsync(AppDbContext db, UserManager<User> userManager)
    {
        if (await db.Users.AnyAsync()) return;

        await SeedUsersAsync(userManager);
        SeedCategories(db);
        SeedTags(db);
        await db.SaveChangesAsync();

        SeedPosts(db);
        await db.SaveChangesAsync();

        SeedComments(db);
        await db.SaveChangesAsync();
    }

    // ── Users ──────────────────────────────────────────────────────────────────

    private static async Task SeedUsersAsync(UserManager<User> userManager)
    {
        var alice = new User
        {
            Id = AliceId,
            UserName = "alice@contoso.com",
            Email = "alice@contoso.com",
            FirstName = "Alice",
            LastName = "Johnson",
            IsActive = true,
            EmailConfirmed = true,
            LastLoginAt = DateTime.UtcNow.AddDays(-1)
        };

        var bob = new User
        {
            Id = BobId,
            UserName = "bob@fabrikam.com",
            Email = "bob@fabrikam.com",
            FirstName = "Bob",
            LastName = "Smith",
            IsActive = true,
            EmailConfirmed = true,
            LastLoginAt = DateTime.UtcNow.AddHours(-6)
        };

        await userManager.CreateAsync(alice, "Password123!");
        await userManager.CreateAsync(bob, "Password123!");
    }

    // ── Categories ─────────────────────────────────────────────────────────────

    private static void SeedCategories(AppDbContext db)
    {
        db.Categories.AddRange(
            new Category { Id = 1, Name = "Technology", Description = "Software, hardware, and everything in between." },
            new Category { Id = 2, Name = "Lifestyle", Description = "Tips and stories for everyday living." },
            new Category { Id = 3, Name = "Science", Description = "Discoveries, research, and the natural world." },
            new Category { Id = 4, Name = "Travel", Description = "Destinations, guides, and travel experiences." }
        );
    }

    // ── Tags ───────────────────────────────────────────────────────────────────

    private static void SeedTags(AppDbContext db)
    {
        db.Tags.AddRange(
            new Tag { Id = 1, Name = "C#" },
            new Tag { Id = 2, Name = ".NET" },
            new Tag { Id = 3, Name = "EF Core" },
            new Tag { Id = 4, Name = "ASP.NET" },
            new Tag { Id = 5, Name = "Azure" },
            new Tag { Id = 6, Name = "Docker" }
        );
    }

    // ── Posts ───────────────────────────────────────────────────────────────────

    private static void SeedPosts(AppDbContext db)
    {
        // Alice's posts
        db.Posts.AddRange(
            new Post
            {
                Id = 1,
                Title = "Getting Started with EfCoreKit",
                Content = "EfCoreKit provides a set of conventions and interceptors that eliminate boilerplate in EF Core projects. In this post we walk through the initial setup.",
                Slug = "getting-started-with-efcorekit",
                IsPublished = true,
                CategoryId = 1,
                UserId = AliceId
            },
            new Post
            {
                Id = 2,
                Title = "Soft Deletes Made Simple",
                Content = "With a single call to EnableSoftDelete(), EfCoreKit automatically converts delete commands to flag updates. No manual WHERE clauses needed.",
                Slug = "soft-deletes-made-simple",
                IsPublished = true,
                CategoryId = 1,
                UserId = AliceId
            },
            new Post
            {
                Id = 3,
                Title = "Understanding Audit Trails",
                Content = "Audit trails keep a record of every change. EfCoreKit's AuditInterceptor intercepts save commands and logs who changed what and when.",
                Slug = "understanding-audit-trails",
                IsPublished = false,
                CategoryId = 3,
                UserId = AliceId
            }
        );

        // Bob's posts
        db.Posts.AddRange(
            new Post
            {
                Id = 4,
                Title = "Top 10 Weekend Getaways",
                Content = "Looking for a quick escape? Here are our favourite weekend destinations that won't break the bank.",
                Slug = "top-10-weekend-getaways",
                IsPublished = true,
                CategoryId = 4,
                UserId = BobId
            },
            new Post
            {
                Id = 5,
                Title = "Healthy Habits for Developers",
                Content = "Long hours at the desk take a toll. These simple habits can make a big difference to your health and productivity.",
                Slug = "healthy-habits-for-developers",
                IsPublished = true,
                CategoryId = 2,
                UserId = BobId
            }
        );

        // Post-Tag associations
        db.PostTags.AddRange(
            new PostTag { PostId = 1, TagId = 2 },  // EfCoreKit → .NET
            new PostTag { PostId = 1, TagId = 3 },  // EfCoreKit → EF Core
            new PostTag { PostId = 2, TagId = 3 },  // Soft Deletes → EF Core
            new PostTag { PostId = 2, TagId = 4 },  // Soft Deletes → ASP.NET
            new PostTag { PostId = 3, TagId = 1 },  // Soft Deletes → C#
            new PostTag { PostId = 3, TagId = 3 },  // Soft Deletes → EF Core
            new PostTag { PostId = 5, TagId = 2 }   // Healthy Habits → .NET
        );
    }

    // ── Comments ───────────────────────────────────────────────────────────────

    private static void SeedComments(AppDbContext db)
    {
        db.Comments.AddRange(
            new Comment
            {
                Id = 1,
                Body = "Great introduction! The interceptor approach is really elegant.",
                PostId = 1,
                UserId = BobId
            },
            new Comment
            {
                Id = 2,
                Body = "Could you cover keyset pagination next?",
                PostId = 1,
                UserId = AliceId
            },
            new Comment
            {
                Id = 3,
                Body = "This saved me hours of writing tenant filters by hand.",
                PostId = 2,
                UserId = BobId
            },
            new Comment
            {
                Id = 4,
                Body = "Adding Bali to my list — thanks for the tips!",
                PostId = 4,
                UserId = AliceId
            }
        );
    }
}
