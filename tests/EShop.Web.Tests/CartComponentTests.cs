using System.Net;
using System.Net.Http.Json;
using Bunit;
using EShop.Web.Components.Pages;
using EShop.Web.Contracts;
using EShop.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EShop.Web.Tests;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Verifies that checkout clears the cart only after a confirmed API success.
public sealed class CartComponentTests
{
    private static readonly Guid ProductId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task CheckoutSuccess_ClearsCartAndNavigatesToConfirmation()
    {
        using var context = new BunitContext();
        var cart = new CartState();
        cart.Add(ProductId, 2);
        ConfigureClients(context, HttpStatusCode.Created);
        context.Services.AddSingleton(cart);

        var component = context.Render<Cart>();
        component.Find("#customer-name").Change("Morgan Ellis");
        component.Find("#customer-email").Change("morgan.ellis@example.com");
        await component.Find("form").SubmitAsync();

        Assert.Equal(0, cart.ItemCount);
        Assert.Contains("/orders/", context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri);
    }

    [Fact]
    public async Task CheckoutFailure_KeepsCartAndShowsError()
    {
        using var context = new BunitContext();
        var cart = new CartState();
        cart.Add(ProductId, 2);
        ConfigureClients(context, HttpStatusCode.Conflict);
        context.Services.AddSingleton(cart);

        var component = context.Render<Cart>();
        component.Find("#customer-name").Change("Morgan Ellis");
        component.Find("#customer-email").Change("morgan.ellis@example.com");
        await component.Find("form").SubmitAsync();

        Assert.Equal(2, cart.ItemCount);
        Assert.Contains("Product availability changed", component.Markup);
    }

    private static void ConfigureClients(BunitContext context, HttpStatusCode checkoutStatus)
    {
        context.Services.AddSingleton(new ProductApiClient(new HttpClient(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create<IReadOnlyList<ProductSummary>>(
                [
                    new ProductSummary(ProductId, "Wireless Mouse", null, "ACC-MOU-001", 29.99m, 45, "Accessories", true)
                ])
            }))
        {
            BaseAddress = new Uri("http://localhost/")
        }));

        context.Services.AddSingleton(new OrderApiClient(new HttpClient(
            new StubHandler(_ => checkoutStatus == HttpStatusCode.Created
                ? new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(new CheckoutOrderResponse(
                        Guid.NewGuid(),
                        "Morgan Ellis",
                        "morgan.ellis@example.com",
                        DateTimeOffset.UtcNow,
                        null,
                        "Pending",
                        [new CheckoutOrderItemResponse(
                            Guid.NewGuid(),
                            ProductId,
                            "Wireless Mouse",
                            2,
                            29.99m,
                            59.98m)],
                        59.98m))
                }
                : new HttpResponseMessage(HttpStatusCode.Conflict)))
        {
            BaseAddress = new Uri("http://localhost/")
        }));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
