using EShop.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShop.Infrastructure.Data.Configurations;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Persists order-line snapshots independently from the current product catalog.
internal sealed class OrderItemEntityConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Ignore(item => item.LineTotal);
        builder.HasIndex(item => item.OrderId);

        builder.HasData(SeedData.OrderItems);
    }
}
