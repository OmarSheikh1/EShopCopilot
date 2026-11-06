using System.Net;
using System.Net.Http.Json;
using EShop.Web.Contracts;

namespace EShop.Web.Services;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Creates checkout orders and requires a valid 201 response before reporting success.
public sealed class OrderApiClient(HttpClient httpClient)
{
    public async Task<CheckoutOrderResponse> CheckoutAsync(
        CheckoutOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/orders/checkout",
            request,
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw ApiClientException.ForCheckoutResponse(response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<CheckoutOrderResponse>(
            cancellationToken);
        return order is { Id: var id } && id != Guid.Empty
            ? order
            : throw new InvalidDataException("The order service returned an invalid checkout response.");
    }
}
