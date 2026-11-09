using OrderModel = EShop.ApiService.Models.Order;

namespace EShop.Infrastructure.Repositories.Order;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Exposes order queries that eagerly load line items where needed.
public interface IOrderRepository : IRepository<OrderModel>
{
    Task<IReadOnlyList<OrderModel>> GetAllWithItemsAsync(
        CancellationToken cancellationToken = default);

    Task<OrderModel?> GetByIdWithItemsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderModel>> GetByCustomerEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
}
