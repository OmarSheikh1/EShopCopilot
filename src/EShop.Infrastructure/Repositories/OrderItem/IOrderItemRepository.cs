using OrderItemModel = EShop.ApiService.Models.OrderItem;

namespace EShop.Infrastructure.Repositories.OrderItem;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Exposes generic persistence operations for order line items.
public interface IOrderItemRepository : IRepository<OrderItemModel>
{
}
