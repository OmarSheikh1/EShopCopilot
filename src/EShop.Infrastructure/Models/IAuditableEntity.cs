namespace EShop.ApiService.Models;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Standardizes persisted audit timestamps and SQLite-compatible optimistic concurrency.
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }

    DateTime? UpdatedAt { get; set; }
}

public interface IConcurrencyTracked
{
    int Version { get; set; }
}
