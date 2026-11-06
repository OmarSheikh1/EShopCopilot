namespace EShop.ApiService.Contracts;

public interface IOrderService
{
    IReadOnlyList<OrderResponse> GetAll();

    OrderResponse? GetById(Guid id);

    OrderResponse Create(CreateOrderRequest request);

    OrderResponse? Update(Guid id, UpdateOrderRequest request);

    bool Delete(Guid id);
}
