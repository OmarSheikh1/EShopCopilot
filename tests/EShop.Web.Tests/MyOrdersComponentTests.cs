using System.Net;
using System.Net.Http.Json;
using Bunit;
using EShop.Web.Components.Pages;
using EShop.Web.Contracts;
using EShop.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EShop.Web.Tests;

// CHG-20261106-002-MY-ORDERS
// Purpose: Verifies the My Orders list and order details pages.
public sealed class MyOrdersComponentTests
{
    private static readonly Guid OrderId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    private static OrderSummary Order(string status = "Shipped") =>
        new(OrderId, "Jordan Lee", "jordan.lee@example.com", DateTimeOffset.UtcNow, null, status,
            [new OrderItemSummary(Guid.NewGuid(), Guid.NewGuid(), "Wireless Mouse", 2, 29.99m, 59.98m)],
            59.98m);

    private static (BunitContext Context, List<string> Requests) Setup(
        Func<HttpRequestMessage, HttpResponseMessage> responder, string? sessionEmail = null)
    {
        var context = new BunitContext();
        var requests = new List<string>();
        context.Services.AddSingleton(new OrderApiClient(new HttpClient(new StubHandler(request =>
        {
            requests.Add(request.RequestUri!.PathAndQuery);
            return responder(request);
        }))
        {
            BaseAddress = new Uri("http://localhost/")
        }));
        var session = new CustomerSession();
        if (sessionEmail is not null)
        {
            session.SetEmail(sessionEmail);
        }

        context.Services.AddSingleton(session);
        return (context, requests);
    }

    [Fact]
    public void MyOrders_WithRememberedEmail_LoadsOrdersWithStatus()
    {
        var (context, requests) = Setup(
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new[] { Order() }) },
            "jordan.lee@example.com");
        using var disposable = context;

        var component = context.Render<MyOrders>();

        component.WaitForAssertion(() => Assert.Contains("Shipped", component.Markup));
        Assert.Contains("Wireless Mouse", component.Markup);
        Assert.Contains("customerEmail=jordan.lee%40example.com", Assert.Single(requests));
    }

    [Fact]
    public void MyOrders_NoOrders_ShowsEmptyState()
    {
        var (context, _) = Setup(
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<OrderSummary>()) },
            "nobody@example.com");
        using var disposable = context;

        var component = context.Render<MyOrders>();

        component.WaitForAssertion(() => Assert.Contains("No orders found", component.Markup));
    }

    [Fact]
    public void MyOrders_ApiError_ShowsErrorMessage()
    {
        var (context, _) = Setup(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError), "a@example.com");
        using var disposable = context;

        var component = context.Render<MyOrders>();

        component.WaitForAssertion(() => Assert.Contains("could not be loaded", component.Markup));
    }

    [Fact]
    public async Task MyOrders_InvalidEmail_DoesNotCallApi()
    {
        var (context, requests) = Setup(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var disposable = context;

        var component = context.Render<MyOrders>();
        component.Find("#lookup-email").Change("not-an-email");
        await component.Find("form").SubmitAsync();

        Assert.Empty(requests);
    }

    [Fact]
    public void OrderDetails_MatchingEmail_ShowsOrder()
    {
        var (context, requests) = Setup(
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Order("Delivered")) },
            "jordan.lee@example.com");
        using var disposable = context;

        var component = context.Render<OrderDetails>(p => p.Add(c => c.OrderId, OrderId));

        component.WaitForAssertion(() => Assert.Contains("Delivered", component.Markup));
        Assert.Contains($"/api/orders/{OrderId}?customerEmail=", Assert.Single(requests));
    }

    [Fact]
    public void OrderDetails_NotFound_ShowsMessage()
    {
        var (context, _) = Setup(_ => new HttpResponseMessage(HttpStatusCode.NotFound), "other@example.com");
        using var disposable = context;

        var component = context.Render<OrderDetails>(p => p.Add(c => c.OrderId, OrderId));

        component.WaitForAssertion(() => Assert.Contains("Order not found for this email", component.Markup));
    }

    [Fact]
    public void OrderDetails_WithoutEmail_PromptsForEmail()
    {
        var (context, requests) = Setup(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var disposable = context;

        var component = context.Render<OrderDetails>(p => p.Add(c => c.OrderId, OrderId));

        Assert.NotNull(component.Find("#order-email"));
        Assert.Empty(requests);
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
