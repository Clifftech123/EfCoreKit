using EfCoreKit.Exceptions;
using EfCoreKit.Repositories;

namespace EfCoreKit.Tests.Integration;

public class RepositoryTests
{
    private static Repository<Product> Repo(BasicDbContext ctx) => new(ctx);

    [Fact]
    public async Task AddAndGetById_RoundTrip()
    {
        await using var ctx = DbFactory.CreateBasic();
        var repo = Repo(ctx);

        await repo.AddAsync(new Product { Name = "Widget", Price = 9.99m });
        await repo.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        var found = await repo.GetByIdAsync(all[0].Id);

        Assert.NotNull(found);
        Assert.Equal("Widget", found!.Name);
    }

    [Fact]
    public async Task GetByIdOrThrowAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        await using var ctx = DbFactory.CreateBasic();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            Repo(ctx).GetByIdOrThrowAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        await using var ctx = DbFactory.CreateBasic();
        var repo = Repo(ctx);

        await repo.AddRangeAsync([
            new Product { Name = "A", Price = 1m },
            new Product { Name = "B", Price = 2m },
            new Product { Name = "C", Price = 3m }
        ]);
        await repo.SaveChangesAsync();

        Assert.Equal(3, (await repo.GetAllAsync()).Count);
    }

    [Fact]
    public async Task FindAsync_Predicate_FiltersCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        var repo = Repo(ctx);

        await repo.AddRangeAsync([
            new Product { Name = "Cheap",     Price = 1m  },
            new Product { Name = "Expensive", Price = 99m }
        ]);
        await repo.SaveChangesAsync();

        var cheap = await repo.FindAsync(p => p.Price < 10m);
        Assert.Single(cheap);
        Assert.Equal("Cheap", cheap[0].Name);
    }

    [Fact]
    public async Task ExistsAsync_ById_ReturnsTrueAndFalseCorrectly()
    {
        await using var ctx = DbFactory.CreateBasic();
        var repo = Repo(ctx);

        await repo.AddAsync(new Product { Name = "Exists", Price = 5m });
        await repo.SaveChangesAsync();

        var id = (await repo.GetAllAsync())[0].Id;

        Assert.True(await repo.ExistsAsync(id));
        Assert.False(await repo.ExistsAsync(id + 999));
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        await using var ctx = DbFactory.CreateBasic();
        var repo = Repo(ctx);

        await repo.AddRangeAsync([
            new Product { Name = "Active",   Price = 1m, IsActive = true  },
            new Product { Name = "Inactive", Price = 2m, IsActive = false },
            new Product { Name = "Active2",  Price = 3m, IsActive = true  }
        ]);
        await repo.SaveChangesAsync();

        Assert.Equal(3, await repo.CountAsync());
        Assert.Equal(2, await repo.CountAsync(p => p.IsActive));
    }

    [Fact]
    public async Task RemoveByIdAsync_RemovesEntity()
    {
        await using var ctx = DbFactory.CreateBasic();
        var repo = Repo(ctx);

        await repo.AddAsync(new Product { Name = "ToDelete", Price = 1m });
        await repo.SaveChangesAsync();

        var id = (await repo.GetAllAsync())[0].Id;
        await repo.RemoveByIdAsync(id);
        await repo.SaveChangesAsync();

        Assert.Empty(await repo.GetAllAsync());
    }

    [Fact]
    public async Task RemoveRangeAsync_Predicate_RemovesMatchingEntities()
    {
        await using var ctx = DbFactory.CreateBasic();
        var repo = Repo(ctx);

        await repo.AddRangeAsync([
            new Product { Name = "Keep",        Price = 10m, IsActive = true  },
            new Product { Name = "DeleteThis1", Price = 1m,  IsActive = false },
            new Product { Name = "DeleteThis2", Price = 2m,  IsActive = false }
        ]);
        await repo.SaveChangesAsync();

        await repo.RemoveRangeAsync(p => !p.IsActive);
        await repo.SaveChangesAsync();

        var remaining = await repo.GetAllAsync();
        Assert.Single(remaining);
        Assert.Equal("Keep", remaining[0].Name);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResult()
    {
        await using var ctx = DbFactory.CreateBasic();
        var repo = Repo(ctx);

        await repo.AddRangeAsync(Enumerable.Range(1, 10)
            .Select(i => new Product { Name = $"P{i}", Price = i }));
        await repo.SaveChangesAsync();

        var page = await repo.GetPagedAsync(page: 2, pageSize: 3);

        Assert.Equal(10, page.TotalCount);
        Assert.Equal(4,  page.TotalPages);
        Assert.Equal(3,  page.Items.Count);
        Assert.Equal(2,  page.Page);
    }
}
