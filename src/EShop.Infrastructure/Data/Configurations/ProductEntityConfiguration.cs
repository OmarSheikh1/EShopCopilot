using EShop.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Data.Configurations;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Defines product constraints, query indexes, and stable catalog seed data.
internal sealed class ProductEntityConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).HasMaxLength(200).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(2000);
        builder.Property(product => product.SKU).HasMaxLength(100).IsRequired();
        builder.Property(product => product.Price).HasPrecision(18, 2);
        builder.Property(product => product.Category).HasMaxLength(100).IsRequired();
        builder.Property(product => product.Version).IsConcurrencyToken();
        builder.HasIndex(product => new { product.Category, product.IsActive });
        builder.HasIndex(product => product.Name);

        builder.HasData(
            new Product
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse with adjustable DPI.",
                SKU = "ACC-MOU-001",
                Price = 29.99m,
                StockQuantity = 45,
                Category = "Accessories",
                CreatedAt = SeedData.ProductCreatedAt
            },
            new Product
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Name = "Mechanical Keyboard",
                Description = "Compact mechanical keyboard with tactile switches.",
                SKU = "ACC-KEY-002",
                Price = 89.50m,
                StockQuantity = 20,
                Category = "Accessories",
                CreatedAt = SeedData.ProductCreatedAt
            },
            new Product
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Name = "USB-C Hub",
                Description = "Seven-port USB-C hub for laptops and tablets.",
                SKU = "ACC-HUB-003",
                Price = 54.00m,
                StockQuantity = 32,
                Category = "Accessories",
                CreatedAt = SeedData.ProductCreatedAt
            },
            new Product
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                Name = "27-inch Monitor",
                Description = "QHD IPS monitor with an adjustable stand.",
                SKU = "DIS-MON-004",
                Price = 279.00m,
                StockQuantity = 12,
                Category = "Displays",
                CreatedAt = SeedData.ProductCreatedAt
            },
            new Product
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                Name = "Laptop Stand",
                Description = "Aluminum stand with adjustable height and angle.",
                SKU = "ACC-STD-005",
                Price = 39.95m,
                StockQuantity = 28,
                Category = "Accessories",
                CreatedAt = SeedData.ProductCreatedAt
            });
    }
}
