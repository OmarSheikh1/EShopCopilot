using System.Net.Http.Json;
using EShop.Web.Contracts;

namespace EShop.Web.Services;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Retrieves product data through the Aspire-discovered API endpoint.
public sealed class ProductApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ProductSummary>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/products/", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw ApiClientException.ForProductResponse(response.StatusCode);
        }

        var products = await response.Content.ReadFromJsonAsync<List<ProductSummary>>(
            cancellationToken);
        return products
            ?? throw new InvalidDataException("The product service returned an empty response.");
    }
}
