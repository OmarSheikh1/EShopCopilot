using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using EShop.ApiService.Contracts;
using EShop.ApiService.Services;

namespace EShop.ApiService.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders")
            .WithTags("Orders");

        // CHG-20261106-001-SHOPPING-CART
        // Purpose: Exposes checkout without accepting client-supplied product pricing.
        orders.MapPost("/checkout", Results<Created<OrderResponse>, NotFound, Conflict<ProblemDetails>> (
                CheckoutOrderRequest request, IOrderService service, CancellationToken cancellationToken) =>
            {
                var result = service.Checkout(request);
                if (result.Order is { } order)
                {
                    return TypedResults.Created($"/api/orders/{order.Id}", order);
                }

                return result.Failure switch
                {
                    CheckoutFailure.ProductNotFound => TypedResults.NotFound(),
                    CheckoutFailure.ProductUnavailable => TypedResults.Conflict(new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Product unavailable",
                        Detail = "One or more products in the cart are no longer available."
                    }),
                    CheckoutFailure.InsufficientStock => TypedResults.Conflict(new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Insufficient stock",
                        Detail = "One or more quantities exceed the available stock."
                    }),
                    _ => throw new InvalidOperationException("Checkout returned neither an order nor a failure.")
                };
            })
            .WithName("CheckoutOrder")
            .WithSummary("Create an order from the shopping cart")
            .WithDescription("Validates product availability and uses current server-side product names and prices.")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapGet("/", (IOrderService service, CancellationToken cancellationToken) =>
                TypedResults.Ok(service.GetAll()))
            .WithName("GetOrders")
            .WithSummary("Get all orders")
            .WithDescription("Returns all orders in the in-memory order catalog.")
            .Produces<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK);

        orders.MapGet("/{id:guid}", Results<Ok<OrderResponse>, NotFound> (
                Guid id, IOrderService service, CancellationToken cancellationToken) =>
            {
                var order = service.GetById(id);
                return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
            })
            .WithName("GetOrderById")
            .WithSummary("Get an order by ID")
            .WithDescription("Returns one order, or 404 when the ID does not exist.")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("/", (CreateOrderRequest request, IOrderService service, CancellationToken cancellationToken) =>
            {
                var order = service.Create(request);
                return TypedResults.Created($"/api/orders/{order.Id}", order);
            })
            .WithName("CreateOrder")
            .WithSummary("Create an order")
            .WithDescription("Adds an order to the in-memory order catalog.")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        orders.MapPut("/{id:guid}", Results<Ok<OrderResponse>, NotFound> (
                Guid id, UpdateOrderRequest request, IOrderService service, CancellationToken cancellationToken) =>
            {
                var order = service.Update(id, request);
                return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
            })
            .WithName("UpdateOrder")
            .WithSummary("Replace an order")
            .WithDescription("Replaces the customer, status, and item fields for the specified order.")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        orders.MapDelete("/{id:guid}", Results<NoContent, NotFound> (
                Guid id, IOrderService service, CancellationToken cancellationToken) =>
            {
                return service.Delete(id) ? TypedResults.NoContent() : TypedResults.NotFound();
            })
            .WithName("DeleteOrder")
            .WithSummary("Delete an order")
            .WithDescription("Removes the order with the specified ID.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
