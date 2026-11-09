using Microsoft.AspNetCore.Http.HttpResults;

using EShop.ApiService.Contracts;
using EShop.ApiService.Services;

namespace EShop.ApiService.Endpoints;

public static class ProductEndpoints
{
    // CHG-20261106-003-DATABASE-INFRASTRUCTURE
    // Change: Awaits database-backed operations and forwards request cancellation.
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products")
            .WithTags("Products");

        products.MapGet("/", async (IProductService service, CancellationToken cancellationToken) =>
                TypedResults.Ok(await service.GetAllAsync(cancellationToken)))
            .WithName("GetProducts")
            .WithSummary("Get all products")
            .WithDescription("Returns the complete product catalog.")
            .Produces<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK);

        products.MapGet("/{id:guid}", async Task<Results<Ok<ProductResponse>, NotFound>> (
                Guid id, IProductService service, CancellationToken cancellationToken) =>
            {
                var product = await service.GetByIdAsync(id, cancellationToken);
                return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
            })
            .WithName("GetProductById")
            .WithSummary("Get a product by ID")
            .WithDescription("Returns one product, or 404 when the ID does not exist.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        products.MapPost("/", async (
            CreateProductRequest request,
            IProductService service,
            CancellationToken cancellationToken) =>
            {
                var product = await service.CreateAsync(request, cancellationToken);
                return TypedResults.Created($"/api/products/{product.Id}", product);
            })
            .WithName("CreateProduct")
            .WithSummary("Create a product")
            .WithDescription("Persists a product in the catalog.")
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        products.MapPut("/{id:guid}", async Task<Results<Ok<ProductResponse>, NotFound>> (
                Guid id,
                UpdateProductRequest request,
                IProductService service,
                CancellationToken cancellationToken) =>
            {
                var product = await service.UpdateAsync(id, request, cancellationToken);
                return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
            })
            .WithName("UpdateProduct")
            .WithSummary("Replace a product")
            .WithDescription("Replaces the product fields for the specified ID.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        products.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
                Guid id,
                IProductService service,
                CancellationToken cancellationToken) =>
            {
                return await service.DeleteAsync(id, cancellationToken)
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound();
            })
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .WithDescription("Removes the product with the specified ID.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
