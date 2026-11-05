namespace EShop.ApiService.Contracts;

public interface IProductService
{
    IReadOnlyList<ProductResponse> GetAll();

    ProductResponse? GetById(Guid id);

    ProductResponse Create(CreateProductRequest request);

    ProductResponse? Update(Guid id, UpdateProductRequest request);

    bool Delete(Guid id);
}
