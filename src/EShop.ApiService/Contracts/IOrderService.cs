namespace EShop.ApiService.Contracts;

public interface IOrderService
{
    IReadOnlyList<OrderResponse> GetAll();

    OrderResponse? GetById(Guid id);

    OrderResponse Create(CreateOrderRequest request);

    // CHG-20261106-001-SHOPPING-CART
    // Change: Adds server-validated checkout while keeping order creation in this service.
    CheckoutOrderResult Checkout(CheckoutOrderRequest request);

    OrderResponse? Update(Guid id, UpdateOrderRequest request);

    bool Delete(Guid id);
}
