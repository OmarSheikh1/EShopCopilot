using EShop.Infrastructure.Data;
using EShop.Infrastructure.Repositories.Specifications;
using OrderModel = EShop.ApiService.Models.Order;

namespace EShop.Infrastructure.Repositories.Order;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Loads orders with their line items for API responses and customer history.
public sealed class OrderRepository(EShopDbContext context)
    : Repository<OrderModel>(context), IOrderRepository
{
    public Task<IReadOnlyList<OrderModel>> GetAllWithItemsAsync(
        CancellationToken cancellationToken = default) =>
        FindAsync(new AllOrdersWithItemsSpec(), cancellationToken);

    public async Task<OrderModel?> GetByIdWithItemsAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        (await FindAsync(new OrderByIdSpec(id), cancellationToken)).SingleOrDefault();

    public Task<IReadOnlyList<OrderModel>> GetByCustomerEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        FindAsync(new OrderByCustomerEmailSpec(email), cancellationToken);
}
