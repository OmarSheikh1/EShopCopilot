using ProductModel = EShop.ApiService.Models.Product;

namespace EShop.Infrastructure.Repositories.Specifications;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Encapsulates common product selection and search expressions.
public sealed class ProductByIdSpec : Specification<ProductModel>
{
    public ProductByIdSpec(Guid id, bool activeOnly = false)
    {
        Criteria = product => product.Id == id && (!activeOnly || product.IsActive);
    }
}

public sealed class ActiveProductsSpec : Specification<ProductModel>
{
    public ActiveProductsSpec() =>
        Criteria = product => product.IsActive;
}

public sealed class ProductByCategorySpec : Specification<ProductModel>
{
    public ProductByCategorySpec(string category) =>
        Criteria = product => product.Category == category;
}

public sealed class ProductSearchSpec : Specification<ProductModel>
{
    public ProductSearchSpec(string searchTerm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        var term = searchTerm.Trim().ToLower();
        Criteria = product => product.IsActive &&
            (product.Name.ToLower().Contains(term) ||
             (product.Description != null && product.Description.ToLower().Contains(term)));
        AddOrdering(product => product.Name, ascending: true);
    }
}
