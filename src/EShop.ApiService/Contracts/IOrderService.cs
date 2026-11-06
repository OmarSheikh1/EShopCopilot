namespace EShop.ApiService.Contracts;

public interface IOrderService
{
    IReadOnlyList<OrderResponse> GetAll();

    OrderResponse? GetById(Guid id);

    // CHG-20261106-002-MY-ORDERS
    // Purpose: Customer-scoped order history and single-order lookup by ID and email.
    IReadOnlyList<OrderResponse> GetByCustomerEmail(string email);

    OrderResponse? GetByIdAndEmail(Guid id, string email);

    OrderResponse Create(CreateOrderRequest request);

    // CHG-20261106-001-SHOPPING-CART
    // Change: Adds server-validated checkout while keeping order creation in this service.
    CheckoutOrderResult Checkout(CheckoutOrderRequest request);

    OrderResponse? Update(Guid id, UpdateOrderRequest request);

    bool Delete(Guid id);
}
