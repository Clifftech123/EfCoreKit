using EfCoreKit.Sample.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Data.Configurations;

public sealed class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> b)
    {
        b.HasKey(pt => new { pt.PostId, pt.TagId });

        b.HasOne(pt => pt.Post).WithMany(p => p.PostTags)
            .HasForeignKey(pt => pt.PostId);

        b.HasOne(pt => pt.Tag).WithMany(t => t.PostTags)
            .HasForeignKey(pt => pt.TagId);
    }
}
