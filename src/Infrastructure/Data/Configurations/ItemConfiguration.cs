using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Infrastructure.Data.Configurations;

public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Item");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Quantity)
            .IsRequired();

        // Related items are always looked up by product, and SQL Server does not index foreign keys on its own.
        builder.HasIndex(item => item.ProductId)
            .HasDatabaseName("IX_Item_ProductId");

        // No cascade: a product that still has items cannot be deleted (matches the original DDL).
        builder.HasOne(item => item.Product)
            .WithMany(product => product.Items)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
