using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EShop.ApiService.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EShop.ApiService.Tests;

// CHG-20261106-002-MY-ORDERS
// Purpose: Verifies customer-scoped order history and ID+email order lookup.
public sealed class OrderHistoryEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string JordanEmail = "jordan.lee@example.com";
    private const string JordanOrderId = "20000000-0000-0000-0000-000000000001";
    private const string TaylorOrderId = "20000000-0000-0000-0000-000000000002";

    [Fact]
    public async Task GetOrders_WithEmail_ReturnsOnlyThatCustomersOrdersCaseInsensitively()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var orders = await client.GetFromJsonAsync<List<OrderResponse>>(
            "/api/orders?customerEmail=JORDAN.LEE@example.com", JsonOptions);

        var order = Assert.Single(orders!);
        Assert.Equal(JordanEmail, order.CustomerEmail);
    }

    [Fact]
    public async Task GetOrders_WithUnknownEmail_ReturnsEmptyList()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var orders = await client.GetFromJsonAsync<List<OrderResponse>>(
            "/api/orders?customerEmail=nobody@example.com", JsonOptions);

        Assert.Empty(orders!);
    }

    [Fact]
    public async Task GetOrders_WithInvalidEmail_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/orders?customerEmail=not-an-email");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithoutEmail_ReturnsAllOrders()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var orders = await client.GetFromJsonAsync<List<OrderResponse>>("/api/orders", JsonOptions);

        Assert.Equal(5, orders!.Count);
    }

    [Fact]
    public async Task GetOrderById_WithMatchingEmail_ReturnsOrder()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var order = await client.GetFromJsonAsync<OrderResponse>(
            $"/api/orders/{JordanOrderId}?customerEmail={JordanEmail}", JsonOptions);

        Assert.Equal(Guid.Parse(JordanOrderId), order!.Id);
    }

    [Fact]
    public async Task GetOrderById_WithAnotherCustomersEmail_ReturnsNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/orders/{TaylorOrderId}?customerEmail={JordanEmail}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_WithInvalidEmail_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/orders/{JordanOrderId}?customerEmail=bad");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_WithoutEmail_StillReturnsOrder()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/orders/{JordanOrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
