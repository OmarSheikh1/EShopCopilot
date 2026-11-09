
namespace EShop.ApiService.Models;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Change: Makes product timestamps and concurrency state persistable by EF Core.
public sealed class Product : IAuditableEntity, IConcurrencyTracked
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public string? Description { get; set; }

    public string SKU { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string Category { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public int Version { get; set; }
}
