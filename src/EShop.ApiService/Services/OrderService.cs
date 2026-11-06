using EShop.ApiService.Contracts;
using EShop.ApiService.Models;

namespace EShop.ApiService.Services;

public sealed class OrderService : IOrderService
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Order> _orders;

    public OrderService()
    {
        var orders = new[]
        {
            CreateSeedOrder(
                "20000000-0000-0000-0000-000000000001",
                "Jordan Lee",
                "jordan.lee@example.com",
                OrderStatus.Confirmed,
                7,
                ("10000000-0000-0000-0000-000000000001", "Wireless Mouse", 2, 29.99m),
                ("10000000-0000-0000-0000-000000000002", "Mechanical Keyboard", 1, 89.50m)),
            CreateSeedOrder(
                "20000000-0000-0000-0000-000000000002",
                "Taylor Morgan",
                "taylor.morgan@example.com",
                OrderStatus.Shipped,
                5,
                ("10000000-0000-0000-0000-000000000004", "27-inch Monitor", 1, 279.00m)),
            CreateSeedOrder(
                "20000000-0000-0000-0000-000000000003",
                "Casey Patel",
                "casey.patel@example.com",
                OrderStatus.Pending,
                2,
                ("10000000-0000-0000-0000-000000000003", "USB-C Hub", 1, 54.00m),
                ("10000000-0000-0000-0000-000000000005", "Laptop Stand", 1, 39.95m)),
            CreateSeedOrder(
                "20000000-0000-0000-0000-000000000004",
                "Riley Chen",
                "riley.chen@example.com",
                OrderStatus.Delivered,
                12,
                ("10000000-0000-0000-0000-000000000002", "Mechanical Keyboard", 1, 89.50m),
                ("10000000-0000-0000-0000-000000000003", "USB-C Hub", 1, 54.00m)),
            CreateSeedOrder(
                "20000000-0000-0000-0000-000000000005",
                "Avery Brooks",
                "avery.brooks@example.com",
                OrderStatus.Cancelled,
                1,
                ("10000000-0000-0000-0000-000000000001", "Wireless Mouse", 1, 29.99m))
        };

        _orders = orders.ToDictionary(order => order.Id);
    }

    public IReadOnlyList<OrderResponse> GetAll()
    {
        lock (_sync)
        {
            return _orders.Values.Select(ToResponse).ToArray();
        }
    }

    public OrderResponse? GetById(Guid id)
    {
        lock (_sync)
        {
            return _orders.TryGetValue(id, out var order) ? ToResponse(order) : null;
        }
    }

    public OrderResponse Create(CreateOrderRequest request)
    {
        var order = new Order
        {
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Items = request.Items.Select(ToModel).ToList()
        };

        lock (_sync)
        {
            _orders.Add(order.Id, order);
            return ToResponse(order);
        }
    }

    public OrderResponse? Update(Guid id, UpdateOrderRequest request)
    {
        lock (_sync)
        {
            if (!_orders.TryGetValue(id, out var current))
            {
                return null;
            }

            var updated = new Order
            {
                Id = current.Id,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Status = request.Status,
                Items = request.Items.Select(ToModel).ToList(),
                CreatedAt = current.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _orders[id] = updated;
            return ToResponse(updated);
        }
    }

    public bool Delete(Guid id)
    {
        lock (_sync)
        {
            return _orders.Remove(id);
        }
    }

    private static Order CreateSeedOrder(
        string id,
        string customerName,
        string customerEmail,
        OrderStatus status,
        int daysAgo,
        params (string ProductId, string ProductName, int Quantity, decimal UnitPrice)[] items) =>
        new()
        {
            Id = Guid.Parse(id),
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-daysAgo),
            Items = items.Select(item => new OrderItem
            {
                ProductId = Guid.Parse(item.ProductId),
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

    private static OrderItem ToModel(OrderItemRequest item) =>
        new()
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        };

    private static OrderResponse ToResponse(Order order) =>
        new(
            order.Id,
            order.CustomerName,
            order.CustomerEmail,
            ToDateTimeOffset(order.CreatedAt),
            order.UpdatedAt is { } updatedAt ? ToDateTimeOffset(updatedAt) : null,
            order.Status,
            order.Items.Select(item => new OrderItemResponse(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal)).ToArray(),
            order.TotalAmount);

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value),
            DateTimeKind.Local => new DateTimeOffset(value),
            _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
        };
}
