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

        var categories = SeedCategories(db);
        var tags = SeedTags(db);
        await db.SaveChangesAsync();

        // After SaveChanges, category/tag IDs are populated by SQL Server.
        var posts = SeedPosts(db, categories, tags);
        await db.SaveChangesAsync();

        SeedComments(db, posts);
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



    private static Category[] SeedCategories(AppDbContext db)
    {
        var categories = new[]
        {
            new Category { Name = "Technology", Description = "Software, hardware, and everything in between." },
            new Category { Name = "Lifestyle", Description = "Tips and stories for everyday living." },
            new Category { Name = "Science", Description = "Discoveries, research, and the natural world." },
            new Category { Name = "Travel", Description = "Destinations, guides, and travel experiences." }
        };
        db.Categories.AddRange(categories);
        return categories;
    }


    private static Tag[] SeedTags(AppDbContext db)
    {
        var tags = new[]
        {
            new Tag { Name = "C#" },
            new Tag { Name = ".NET" },
            new Tag { Name = "EF Core" },
            new Tag { Name = "ASP.NET" },
            new Tag { Name = "Azure" },
            new Tag { Name = "Docker" }
        };
        db.Tags.AddRange(tags);
        return tags;
    }



    private static Post[] SeedPosts(AppDbContext db, Category[] categories, Tag[] tags)
    {
        var tech = categories[0];
        var lifestyle = categories[1];
        var science = categories[2];
        var travel = categories[3];

        var csharp = tags[0];
        var dotnet = tags[1];
        var efcore = tags[2];
        var aspnet = tags[3];

        var posts = new[]
        {
            // Alice's posts
            new Post
            {
                Title = "Getting Started with EfCoreKit",
                Content = "EfCoreKit provides a set of conventions and interceptors that eliminate boilerplate in EF Core projects. In this post we walk through the initial setup.",
                Slug = "getting-started-with-efcorekit",
                IsPublished = true,
                Category = tech,
                UserId = AliceId,
                PostTags = [new PostTag { Tag = dotnet }, new PostTag { Tag = efcore }]
            },
            new Post
            {
                Title = "Soft Deletes Made Simple",
                Content = "With a single call to EnableSoftDelete(), EfCoreKit automatically converts delete commands to flag updates. No manual WHERE clauses needed.",
                Slug = "soft-deletes-made-simple",
                IsPublished = true,
                Category = tech,
                UserId = AliceId,
                PostTags = [new PostTag { Tag = efcore }, new PostTag { Tag = aspnet }]
            },
            new Post
            {
                Title = "Understanding Audit Trails",
                Content = "Audit trails keep a record of every change. EfCoreKit's AuditInterceptor intercepts save commands and logs who changed what and when.",
                Slug = "understanding-audit-trails",
                IsPublished = false,
                Category = science,
                UserId = AliceId,
                PostTags = [new PostTag { Tag = csharp }, new PostTag { Tag = efcore }]
            },
            // Bob's posts
            new Post
            {
                Title = "Top 10 Weekend Getaways",
                Content = "Looking for a quick escape? Here are our favourite weekend destinations that won't break the bank.",
                Slug = "top-10-weekend-getaways",
                IsPublished = true,
                Category = travel,
                UserId = BobId
            },
            new Post
            {
                Title = "Healthy Habits for Developers",
                Content = "Long hours at the desk take a toll. These simple habits can make a big difference to your health and productivity.",
                Slug = "healthy-habits-for-developers",
                IsPublished = true,
                Category = lifestyle,
                UserId = BobId,
                PostTags = [new PostTag { Tag = dotnet }]
            }
        };

        db.Posts.AddRange(posts);
        return posts;
    }

    // ── Comments ───────────────────────────────────────────────────────────────

    private static void SeedComments(AppDbContext db, Post[] posts)
    {
        db.Comments.AddRange(
            new Comment
            {
                Body = "Great introduction! The interceptor approach is really elegant.",
                Post = posts[0],
                UserId = BobId
            },
            new Comment
            {
                Body = "Could you cover keyset pagination next?",
                Post = posts[0],
                UserId = AliceId
            },
            new Comment
            {
                Body = "This saved me hours of writing soft-delete filters by hand.",
                Post = posts[1],
                UserId = BobId
            },
            new Comment
            {
                Body = "Adding Bali to my list — thanks for the tips!",
                Post = posts[3],
                UserId = AliceId
            }
        );
    }
}
