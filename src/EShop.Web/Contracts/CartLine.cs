namespace EShop.Web.Contracts;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Keeps cart state limited to product identity and requested quantity.
public sealed record CartLine(Guid ProductId, int Quantity);

public sealed record CartSnapshot(long Version, IReadOnlyList<CartLine> Items);
