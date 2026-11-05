using ProductModel = EShop.ApiService.Models.Product;
using EShop.ApiService.Contracts;

namespace EShop.ApiService.Services;

public sealed class ProductService : IProductService
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ProductModel> _products;

    public ProductService()
    {
        var products = new[]
        {
            new ProductModel
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse with adjustable DPI.",
                SKU = "ACC-MOU-001",
                Price = 29.99m,
                StockQuantity = 45,
                Category = "Accessories"
            },
            new ProductModel
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Name = "Mechanical Keyboard",
                Description = "Compact mechanical keyboard with tactile switches.",
                SKU = "ACC-KEY-002",
                Price = 89.50m,
                StockQuantity = 20,
                Category = "Accessories"
            },
            new ProductModel
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Name = "USB-C Hub",
                Description = "Seven-port USB-C hub for laptops and tablets.",
                SKU = "ACC-HUB-003",
                Price = 54.00m,
                StockQuantity = 32,
                Category = "Accessories"
            },
            new ProductModel
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                Name = "27-inch Monitor",
                Description = "QHD IPS monitor with an adjustable stand.",
                SKU = "DIS-MON-004",
                Price = 279.00m,
                StockQuantity = 12,
                Category = "Displays"
            },
            new ProductModel
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                Name = "Laptop Stand",
                Description = "Aluminum stand with adjustable height and angle.",
                SKU = "ACC-STD-005",
                Price = 39.95m,
                StockQuantity = 28,
                Category = "Accessories"
            }
        };

        _products = products.ToDictionary(product => product.Id);
    }

    public IReadOnlyList<ProductResponse> GetAll()
    {
        lock (_sync)
        {
            return _products.Values.Select(ToResponse).ToArray();
        }
    }

    public ProductResponse? GetById(Guid id)
    {
        lock (_sync)
        {
            return _products.TryGetValue(id, out var product) ? ToResponse(product) : null;
        }
    }

    public ProductResponse Create(CreateProductRequest request)
    {
        var product = new ProductModel
        {
            Name = request.Name,
            Description = request.Description,
            SKU = request.SKU,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = request.Category,
            IsActive = request.IsActive
        };

        lock (_sync)
        {
            _products.Add(product.Id, product);
            return ToResponse(product);
        }
    }

    public ProductResponse? Update(Guid id, UpdateProductRequest request)
    {
        lock (_sync)
        {
            if (!_products.TryGetValue(id, out var current))
            {
                return null;
            }

            var updated = new ProductModel
            {
                Id = current.Id,
                Name = request.Name,
                Description = request.Description,
                SKU = request.SKU,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                Category = request.Category,
                IsActive = request.IsActive,
                CreatedAt = current.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _products[id] = updated;
            return ToResponse(updated);
        }
    }

    public bool Delete(Guid id)
    {
        lock (_sync)
        {
            return _products.Remove(id);
        }
    }

    private static ProductResponse ToResponse(ProductModel product) =>
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
