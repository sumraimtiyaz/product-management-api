using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.ProductName)
            .IsRequired()
            .HasMaxLength(Product.NameMaxLength);

        builder.Property(product => product.CreatedBy)
            .IsRequired()
            .HasMaxLength(Product.AuditUserMaxLength);

        builder.Property(product => product.CreatedOn)
            .HasColumnType("datetime");

        builder.Property(product => product.ModifiedBy)
            .HasMaxLength(Product.AuditUserMaxLength);

        builder.Property(product => product.ModifiedOn)
            .HasColumnType("datetime");

        builder.Navigation(product => product.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
