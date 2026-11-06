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
// Purpose: Verifies the products page adds available products to circuit cart state.
public sealed class ProductComponentTests
{
    [Fact]
    public void AddProduct_UpdatesCartCount()
    {
        using var context = new BunitContext();
        var cart = new CartState();
        context.Services.AddSingleton(cart);
        context.Services.AddSingleton(new ProductApiClient(new HttpClient(
            new ProductHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        }));

        var component = context.Render<Products>();
        component.Find("button.btn-primary").Click();

        Assert.Equal(1, cart.ItemCount);
        Assert.Contains("added to your cart", component.Markup);
    }

    private sealed class ProductHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create<IReadOnlyList<ProductSummary>>(
                [
                    new ProductSummary(
                        Guid.Parse("10000000-0000-0000-0000-000000000001"),
                        "Wireless Mouse",
                        "A test product.",
                        "ACC-MOU-001",
                        29.99m,
                        45,
                        "Accessories",
                        true)
                ])
            });
    }
}
