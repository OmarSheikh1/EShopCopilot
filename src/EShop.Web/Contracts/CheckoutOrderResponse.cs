namespace EShop.Web.Contracts;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Describes the order created by the checkout API.
public sealed record CheckoutOrderResponse(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string Status,
    IReadOnlyList<CheckoutOrderItemResponse> Items,
    decimal TotalAmount);

public sealed record CheckoutOrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
