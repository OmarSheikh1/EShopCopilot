using EShop.Infrastructure.Data;
using OrderItemModel = EShop.ApiService.Models.OrderItem;

namespace EShop.Infrastructure.Repositories.OrderItem;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Provides the concrete EF Core order-item repository.
public sealed class OrderItemRepository(EShopDbContext context)
    : Repository<OrderItemModel>(context), IOrderItemRepository;
