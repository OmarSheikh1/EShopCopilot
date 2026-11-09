using ProductModel = EShop.ApiService.Models.Product;

namespace EShop.Infrastructure.Repositories.Product;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Exposes product-specific queries over the generic repository contract.
public interface IProductRepository : IRepository<ProductModel>
{
    Task<IReadOnlyList<ProductModel>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);
}
