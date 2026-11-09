using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EShop.ApiService.Contracts;
using EShop.ApiService.Models;
using EShop.ApiService.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EShop.ApiService.Tests;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Verifies seeded, created, updated, and deleted data through SQLite-backed HTTP endpoints.
public sealed class DatabasePersistenceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task ProductAndOrderChangesPersistAcrossApiRestarts()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"eshop-persistence-{Guid.NewGuid():N}.db");
        Guid productId;
        Guid orderId;

        try
        {
            await using (var firstFactory = new DatabaseTestApplicationFactory(databasePath))
            using (var client = firstFactory.CreateClient())
            {
                using var healthResponse = await client.GetAsync("/health");
                Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
                using var openApiResponse = await client.GetAsync("/openapi/v1.json");
                Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);
                var openApiDocument = await openApiResponse.Content.ReadAsStringAsync();
                Assert.Contains("/api/products", openApiDocument, StringComparison.Ordinal);
                Assert.Contains("/api/orders", openApiDocument, StringComparison.Ordinal);

                var seededProduct = await client.GetFromJsonAsync<ProductResponse>(
                    "/api/products/10000000-0000-0000-0000-000000000001");
                Assert.Equal("Wireless Mouse", seededProduct!.Name);

                using var productResponse = await client.PostAsJsonAsync(
                    "/api/products/",
                    new CreateProductRequest
                    {
                        Name = "USB-C Dock",
                        Description = "A persistent dock.",
                        SKU = "ACC-DCK-006",
                        Price = 129.99m,
                        StockQuantity = 15,
                        Category = "Accessories"
                    });
                Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
                var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>();
                productId = product!.Id;

                using var orderResponse = await client.PostAsJsonAsync(
                    "/api/orders/",
                    new CreateOrderRequest
                    {
                        CustomerName = "Morgan Ellis",
                        CustomerEmail = "morgan.ellis@example.com",
                        Items =
                        [
                            new OrderItemRequest
                            {
                                ProductId = productId,
                                ProductName = product.Name,
                                Quantity = 2,
                                UnitPrice = product.Price
                            },
                            new OrderItemRequest
                            {
                                ProductId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                                ProductName = "Wireless Mouse",
                                Quantity = 1,
                                UnitPrice = 29.99m
                            }
                        ]
                    });
                Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
                var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
                orderId = order!.Id;
                Assert.Equal(289.97m, order.TotalAmount);

                using var productUpdateResponse = await client.PutAsJsonAsync(
                    $"/api/products/{productId}",
                    new UpdateProductRequest
                    {
                        Name = "USB-C Dock Plus",
                        Description = "Updated persistent dock.",
                        SKU = "ACC-DCK-006",
                        Price = 139.99m,
                        StockQuantity = 10,
                        Category = "Accessories",
                        IsActive = true
                    });
                Assert.Equal(HttpStatusCode.OK, productUpdateResponse.StatusCode);
            }

            await using (var secondFactory = new DatabaseTestApplicationFactory(databasePath))
            using (var client = secondFactory.CreateClient())
            {
                var product = await client.GetFromJsonAsync<ProductResponse>(
                    $"/api/products/{productId}");
                Assert.Equal("USB-C Dock Plus", product!.Name);
                Assert.Equal(139.99m, product.Price);
                Assert.NotNull(product.UpdatedAt);

                var order = await client.GetFromJsonAsync<OrderResponse>(
                    $"/api/orders/{orderId}", JsonOptions);
                Assert.Equal(289.97m, order!.TotalAmount);
                Assert.Equal(2, order.Items.Count);
                Assert.Contains(order.Items, item => item.ProductId == productId);

                using var orderUpdateResponse = await client.PutAsJsonAsync(
                    $"/api/orders/{orderId}",
                    new UpdateOrderRequest
                    {
                        CustomerName = "Morgan Ellis",
                        CustomerEmail = "morgan.ellis@example.com",
                        Status = OrderStatus.Confirmed,
                        Items =
                        [
                            new OrderItemRequest
                            {
                                ProductId = productId,
                                ProductName = product.Name,
                                Quantity = 1,
                                UnitPrice = product.Price
                            }
                        ]
                    },
                    JsonOptions);
                Assert.True(
                    orderUpdateResponse.StatusCode == HttpStatusCode.OK,
                    await orderUpdateResponse.Content.ReadAsStringAsync());
                var updatedOrder = await orderUpdateResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
                Assert.NotNull(updatedOrder);
                Assert.Equal(139.99m, updatedOrder.TotalAmount);
                Assert.Equal(OrderStatus.Confirmed, updatedOrder.Status);
                Assert.Equal(productId, Assert.Single(updatedOrder.Items).ProductId);

                using var deleteResponse = await client.DeleteAsync($"/api/orders/{orderId}");
                Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
                using var missingOrderResponse = await client.GetAsync($"/api/orders/{orderId}");
                Assert.Equal(HttpStatusCode.NotFound, missingOrderResponse.StatusCode);
            }
        }
        finally
        {
            DatabaseTestApplicationFactory.DeleteDatabaseFiles(databasePath);
        }
    }

    [Fact]
    public async Task QueryFilterHelperTranslatesSearchSortingAndPagingToSql()
    {
        await using var factory = new DatabaseTestApplicationFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<EShop.Infrastructure.Repositories.IUnitOfWork>();

        var specification = new EShop.Infrastructure.Filters.QueryFilterHelper<Product>()
            .AddFilter(product => product.IsActive)
            .AddTextSearch(
                new EShop.Infrastructure.Filters.TextSearchFilter("a"),
                product => product.Name,
                product => product.Description)
            .OrderBy(product => product.Price)
            .Paginate(new EShop.Infrastructure.Filters.PaginationOptions(PageNumber: 1, PageSize: 2))
            .BuildSpecification();

        var products = await unitOfWork.Products.FindAsync(specification, CancellationToken.None);
        Assert.Equal(2, products.Count);
        Assert.Equal("Wireless Mouse", products[0].Name);
        Assert.Equal("Laptop Stand", products[1].Name);
    }

    [Fact]
    public async Task ConcurrentProductUpdatesRejectTheStaleWrite()
    {
        await using var factory = new DatabaseTestApplicationFactory();
        await using var firstScope = factory.Services.CreateAsyncScope();
        await using var secondScope = factory.Services.CreateAsyncScope();
        var firstUnitOfWork = firstScope.ServiceProvider
            .GetRequiredService<EShop.Infrastructure.Repositories.IUnitOfWork>();
        var secondUnitOfWork = secondScope.ServiceProvider
            .GetRequiredService<EShop.Infrastructure.Repositories.IUnitOfWork>();
        var productId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var firstCopy = await firstUnitOfWork.Products.GetByIdAsync(
            productId,
            CancellationToken.None);
        var secondCopy = await secondUnitOfWork.Products.GetByIdAsync(
            productId,
            CancellationToken.None);
        Assert.NotNull(firstCopy);
        Assert.NotNull(secondCopy);

        firstCopy.Price = 30m;
        firstUnitOfWork.Products.Update(firstCopy);
        await firstUnitOfWork.SaveChangesAsync(CancellationToken.None);

        secondCopy.Price = 31m;
        secondUnitOfWork.Products.Update(secondCopy);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None));
    }
}
