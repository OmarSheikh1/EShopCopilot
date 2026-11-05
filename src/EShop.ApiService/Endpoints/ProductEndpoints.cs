using Microsoft.AspNetCore.Http.HttpResults;

using EShop.ApiService.Contracts;
using EShop.ApiService.Services;

namespace EShop.ApiService.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products")
            .WithTags("Products");

        products.MapGet("/", (IProductService service, CancellationToken cancellationToken) =>
                TypedResults.Ok(service.GetAll()))
            .WithName("GetProducts")
            .WithSummary("Get all products")
            .WithDescription("Returns the complete product catalog.")
            .Produces<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK);

        products.MapGet("/{id:guid}", Results<Ok<ProductResponse>, NotFound> (
                Guid id, IProductService service, CancellationToken cancellationToken) =>
            {
                var product = service.GetById(id);
                return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
            })
            .WithName("GetProductById")
            .WithSummary("Get a product by ID")
            .WithDescription("Returns one product, or 404 when the ID does not exist.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        products.MapPost("/", (CreateProductRequest request, IProductService service, CancellationToken cancellationToken) =>
            {
                var product = service.Create(request);
                return TypedResults.Created($"/api/products/{product.Id}", product);
            })
            .WithName("CreateProduct")
            .WithSummary("Create a product")
            .WithDescription("Adds a product to the in-memory catalog.")
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        products.MapPut("/{id:guid}", Results<Ok<ProductResponse>, NotFound> (
                Guid id, UpdateProductRequest request, IProductService service, CancellationToken cancellationToken) =>
            {
                var product = service.Update(id, request);
                return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
            })
            .WithName("UpdateProduct")
            .WithSummary("Replace a product")
            .WithDescription("Replaces the product fields for the specified ID.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        products.MapDelete("/{id:guid}", Results<NoContent, NotFound> (
                Guid id, IProductService service, CancellationToken cancellationToken) =>
            {
                return service.Delete(id) ? TypedResults.NoContent() : TypedResults.NotFound();
            })
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .WithDescription("Removes the product with the specified ID.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
