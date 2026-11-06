
namespace EShop.ApiService.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string CustomerName { get; set; }

    public required string CustomerEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public List<OrderItem> Items { get; set; } = [];

    public decimal TotalAmount => Items.Sum(
        item => item.UnitPrice * item.Quantity);
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
