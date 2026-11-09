using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EShop.ApiService.Contracts;
using EShop.ApiService.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EShop.ApiService.Tests;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Change: Runs checkout assertions against isolated, migrated SQLite databases.
// CHG-20261106-001-SHOPPING-CART
// Purpose: Verifies checkout validation and server-side order pricing over HTTP.
public sealed class CheckoutEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Guid WirelessMouseId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Checkout_UsesServerProductNameAndPrice()
    {
        await using var factory = new DatabaseTestApplicationFactory();
        using var client = factory.CreateClient();
        var product = await client.GetFromJsonAsync<ProductResponse>(
            $"/api/products/{WirelessMouseId}", JsonOptions);
        Assert.NotNull(product);
        Assert.True(product.IsActive);
        Assert.Equal(45, product.StockQuantity);
        using var response = await client.PostAsJsonAsync(
            "/api/orders/checkout",
            ValidRequest(quantity: 2));

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
        Assert.NotNull(response.Headers.Location);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal("Wireless Mouse", order.Items[0].ProductName);
        Assert.Equal(29.99m, order.Items[0].UnitPrice);
        Assert.Equal(59.98m, order.TotalAmount);
    }

    [Fact]
    public async Task Checkout_RejectsUnknownProductWithoutCreatingOrder()
    {
        await using var factory = new DatabaseTestApplicationFactory();
        using var client = factory.CreateClient();
        var request = ValidRequest(quantity: 1) with
        {
            Items = [new CheckoutOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }]
        };

        using var response = await client.PostAsJsonAsync("/api/orders/checkout", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var orders = await client.GetFromJsonAsync<List<OrderResponse>>("/api/orders/", JsonOptions);
        Assert.NotNull(orders);
        Assert.Equal(5, orders.Count);
    }

    [Fact]
    public async Task Checkout_RejectsInactiveProducts()
    {
        await using var factory = new DatabaseTestApplicationFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var products = scope.ServiceProvider.GetRequiredService<IProductService>();
        var current = Assert.IsType<ProductResponse>(
            await products.GetByIdAsync(WirelessMouseId, CancellationToken.None));
        await products.UpdateAsync(WirelessMouseId, new UpdateProductRequest
        {
            Name = current.Name,
            Description = current.Description,
            SKU = current.SKU,
            Price = current.Price,
            StockQuantity = current.StockQuantity,
            Category = current.Category,
            IsActive = false
        }, CancellationToken.None);

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/orders/checkout",
            ValidRequest(quantity: 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_RejectsInvalidQuantityBeforeCreatingOrder()
    {
        await using var factory = new DatabaseTestApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/orders/checkout",
            ValidRequest(quantity: 46));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var orders = await client.GetFromJsonAsync<List<OrderResponse>>("/api/orders/", JsonOptions);
        Assert.NotNull(orders);
        Assert.Equal(5, orders.Count);
    }

    [Fact]
    public async Task Checkout_RejectsEmptyCartWithValidationProblem()
    {
        await using var factory = new DatabaseTestApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/orders/checkout",
            ValidRequest(quantity: 1) with { Items = [] });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static CheckoutOrderRequest ValidRequest(int quantity) =>
        new()
        {
            CustomerName = "Morgan Ellis",
            CustomerEmail = "morgan.ellis@example.com",
            Items = [new CheckoutOrderItemRequest { ProductId = WirelessMouseId, Quantity = quantity }]
        };
}
