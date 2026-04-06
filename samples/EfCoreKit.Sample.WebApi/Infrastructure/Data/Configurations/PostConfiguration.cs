using EfCoreKit.Sample.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Data.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.HasIndex(p => p.Slug).IsUnique();

        // RowVersion as SQL Server timestamp — EfCoreKit raises ConcurrencyConflictException
        // when a stale version is detected during update.
        b.Property(p => p.RowVersion).IsRowVersion();

        b.HasOne(p => p.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // User → Post relationship is configured in UserConfiguration
    }
}
