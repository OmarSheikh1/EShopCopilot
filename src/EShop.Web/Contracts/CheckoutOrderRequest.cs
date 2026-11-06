using System.ComponentModel.DataAnnotations;

namespace EShop.Web.Contracts;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Sends checkout details and product quantities, never client-calculated prices.
public sealed record CheckoutOrderRequest(
    [property: Required, StringLength(200)] string CustomerName,
    [property: Required, EmailAddress, StringLength(320)] string CustomerEmail,
    [property: Required, MinLength(1)] IReadOnlyList<CheckoutOrderItemRequest> Items);

public sealed record CheckoutOrderItemRequest(Guid ProductId, int Quantity);
