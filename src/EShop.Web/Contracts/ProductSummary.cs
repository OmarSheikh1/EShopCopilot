namespace EShop.Web.Contracts;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Carries product data needed by the storefront without exposing API entities.
public sealed record ProductSummary(
    Guid Id,
    string Name,
    string? Description,
    string SKU,
    decimal Price,
    int StockQuantity,
    string Category,
    bool IsActive);
