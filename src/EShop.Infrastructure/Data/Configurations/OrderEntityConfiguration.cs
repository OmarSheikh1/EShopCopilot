using EShop.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Data.Configurations;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Defines order relationships, constraints, indexes, and stable order seed data.
internal sealed class OrderEntityConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(order => order.CustomerEmail).HasMaxLength(320).IsRequired();
        builder.Property(order => order.Status).HasConversion<int>();
        builder.Property(order => order.Version).IsConcurrencyToken();
        builder.Ignore(order => order.TotalAmount);
        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(order => order.CustomerEmail);
        builder.HasIndex(order => order.CreatedAt);
        builder.HasIndex(order => order.Status);

        builder.HasData(
            SeedData.Order(
                "20000000-0000-0000-0000-000000000001",
                "Jordan Lee",
                "jordan.lee@example.com",
                OrderStatus.Confirmed,
                7),
            SeedData.Order(
                "20000000-0000-0000-0000-000000000002",
                "Taylor Morgan",
                "taylor.morgan@example.com",
                OrderStatus.Shipped,
                5),
            SeedData.Order(
                "20000000-0000-0000-0000-000000000003",
                "Casey Patel",
                "casey.patel@example.com",
                OrderStatus.Pending,
                2),
            SeedData.Order(
                "20000000-0000-0000-0000-000000000004",
                "Riley Chen",
                "riley.chen@example.com",
                OrderStatus.Delivered,
                12),
            SeedData.Order(
                "20000000-0000-0000-0000-000000000005",
                "Avery Brooks",
                "avery.brooks@example.com",
                OrderStatus.Cancelled,
                1));
    }
}
