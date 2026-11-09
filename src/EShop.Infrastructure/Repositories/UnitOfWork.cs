using EShop.Infrastructure.Data;
using EShop.Infrastructure.Repositories.Order;
using EShop.Infrastructure.Repositories.OrderItem;
using EShop.Infrastructure.Repositories.Product;
using Microsoft.EntityFrameworkCore.Storage;

namespace EShop.Infrastructure.Repositories;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Shares one DbContext across repositories and transaction boundaries.
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly EShopDbContext _context;

    public UnitOfWork(
        EShopDbContext context,
        IProductRepository products,
        IOrderRepository orders,
        IOrderItemRepository orderItems)
    {
        _context = context;
        Products = products;
        Orders = orders;
        OrderItems = orderItems;
    }

    public IProductRepository Products { get; }

    public IOrderRepository Orders { get; }

    public IOrderItemRepository OrderItems { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default) =>
        new UnitOfWorkTransaction(await _context.Database.BeginTransactionAsync(cancellationToken));

    public ValueTask DisposeAsync() => _context.DisposeAsync();

    private sealed class UnitOfWorkTransaction(IDbContextTransaction transaction)
        : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default) =>
            transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
