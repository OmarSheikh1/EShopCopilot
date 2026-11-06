
namespace EShop.ApiService.Models;

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public required string ProductName { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
