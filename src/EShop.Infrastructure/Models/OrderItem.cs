
namespace EShop.ApiService.Models;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Change: Adds the persisted order relationship while preserving catalog snapshots.
public sealed class OrderItem : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public required string ProductName { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
}
