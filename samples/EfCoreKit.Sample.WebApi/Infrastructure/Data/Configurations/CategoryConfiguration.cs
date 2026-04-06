using EfCoreKit.Sample.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCoreKit.Sample.WebApi.Infrastructure.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.HasIndex(c => c.Name).IsUnique();
        b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        b.Property(c => c.Description).HasMaxLength(1000);
    }
}
