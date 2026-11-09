using EShop.Infrastructure.Data;
using EShop.Infrastructure.Repositories.Specifications;
using ProductModel = EShop.ApiService.Models.Product;

namespace EShop.Infrastructure.Repositories.Product;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Adds catalog search to the generic product repository.
public sealed class ProductRepository(EShopDbContext context)
    : Repository<ProductModel>(context), IProductRepository
{
    public Task<IReadOnlyList<ProductModel>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default) =>
        FindAsync(new ProductSearchSpec(searchTerm), cancellationToken);
}
