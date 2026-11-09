using EShop.Infrastructure.Repositories.Order;
using EShop.Infrastructure.Repositories.OrderItem;
using EShop.Infrastructure.Repositories.Product;

namespace EShop.Infrastructure.Repositories;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Coordinates repository access and transaction commits for one request.
public interface IUnitOfWork : IAsyncDisposable
{
    IProductRepository Products { get; }

    IOrderRepository Orders { get; }

    IOrderItemRepository OrderItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
