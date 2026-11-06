namespace EShop.Web.Contracts;

// CHG-20261106-002-MY-ORDERS
// Purpose: Describes an order returned by the order history and order lookup APIs.
public sealed record OrderSummary(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string Status,
    IReadOnlyList<OrderItemSummary> Items,
    decimal TotalAmount);

public sealed record OrderItemSummary(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
