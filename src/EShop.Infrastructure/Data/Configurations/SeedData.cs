using EShop.ApiService.Models;

namespace EShop.Infrastructure.Data.Configurations;

internal static class SeedData
{
    internal static readonly DateTime ProductCreatedAt =
        new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    internal static Order Order(
        string id,
        string customerName,
        string customerEmail,
        OrderStatus status,
        int daysAgo) =>
        new()
        {
            Id = Guid.Parse(id),
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Status = status,
            CreatedAt = ProductCreatedAt.AddDays(-daysAgo)
        };

    internal static readonly OrderItem[] OrderItems =
    [
        Item(1, 1, "10000000-0000-0000-0000-000000000001", "Wireless Mouse", 2, 29.99m),
        Item(1, 2, "10000000-0000-0000-0000-000000000002", "Mechanical Keyboard", 1, 89.50m),
        Item(2, 1, "10000000-0000-0000-0000-000000000004", "27-inch Monitor", 1, 279.00m),
        Item(3, 1, "10000000-0000-0000-0000-000000000003", "USB-C Hub", 1, 54.00m),
        Item(3, 2, "10000000-0000-0000-0000-000000000005", "Laptop Stand", 1, 39.95m),
        Item(4, 1, "10000000-0000-0000-0000-000000000002", "Mechanical Keyboard", 1, 89.50m),
        Item(4, 2, "10000000-0000-0000-0000-000000000003", "USB-C Hub", 1, 54.00m),
        Item(5, 1, "10000000-0000-0000-0000-000000000001", "Wireless Mouse", 1, 29.99m)
    ];

    private static OrderItem Item(
        int orderNumber,
        int itemNumber,
        string productId,
        string productName,
        int quantity,
        decimal unitPrice) =>
        new()
        {
            Id = Guid.Parse($"30000000-0000-0000-000{orderNumber}-00000000000{itemNumber}"),
            OrderId = Guid.Parse($"20000000-0000-0000-0000-00000000000{orderNumber}"),
            ProductId = Guid.Parse(productId),
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            CreatedAt = ProductCreatedAt
        };
}
