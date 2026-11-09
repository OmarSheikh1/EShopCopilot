using EShop.ApiService.Contracts;
using EShop.ApiService.Models;
using EShop.Infrastructure.Repositories;

namespace EShop.ApiService.Services;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Implements product operations through the persistent unit of work.
public sealed class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken) =>
        (await unitOfWork.Products.GetAllAsync(cancellationToken))
        .Select(ToResponse)
        .ToArray();

    public async Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        return product is null ? null : ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            SKU = request.SKU,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = request.Category,
            IsActive = request.IsActive
        };

        await unitOfWork.Products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.SKU = request.SKU;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.Category = request.Category;
        product.IsActive = request.IsActive;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(product);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.Products.DeleteAsync(id, cancellationToken))
        {
            return false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProductResponse ToResponse(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.SKU,
            product.Price,
            product.StockQuantity,
            product.Category,
            product.IsActive,
            ToDateTimeOffset(product.CreatedAt),
            product.UpdatedAt is { } updatedAt ? ToDateTimeOffset(updatedAt) : null);

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value),
            DateTimeKind.Local => new DateTimeOffset(value),
            _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
        };
}
