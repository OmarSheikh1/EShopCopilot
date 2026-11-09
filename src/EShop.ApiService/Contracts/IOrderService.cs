namespace EShop.ApiService.Contracts;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Change: Order operations now expose asynchronous cancellation-aware persistence calls.
public interface IOrderService
{
    Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // CHG-20261106-002-MY-ORDERS
    // Purpose: Customer-scoped order history and single-order lookup by ID and email.
    Task<IReadOnlyList<OrderResponse>> GetByCustomerEmailAsync(
        string email,
        CancellationToken cancellationToken);

    Task<OrderResponse?> GetByIdAndEmailAsync(
        Guid id,
        string email,
        CancellationToken cancellationToken);

    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);

    // CHG-20261106-001-SHOPPING-CART
    // Change: Adds server-validated checkout while keeping order creation in this service.
    Task<CheckoutOrderResult> CheckoutAsync(
        CheckoutOrderRequest request,
        CancellationToken cancellationToken);

    Task<OrderResponse?> UpdateAsync(
        Guid id,
        UpdateOrderRequest request,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
