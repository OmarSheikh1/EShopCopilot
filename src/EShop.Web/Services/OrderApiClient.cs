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

    // CHG-20261106-002-MY-ORDERS
    // Purpose: Loads one customer's orders; the email is always sent so other customers' orders are never fetched.
    public async Task<IReadOnlyList<OrderSummary>> GetOrdersByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/orders?customerEmail={Uri.EscapeDataString(email)}",
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw ApiClientException.ForOrdersResponse(response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<List<OrderSummary>>(cancellationToken)
            ?? throw new InvalidDataException("The order service returned an empty order list.");
    }

    // CHG-20261106-002-MY-ORDERS
    // Purpose: Loads a single order only when both ID and email match.
    public async Task<OrderSummary> GetOrderAsync(
        Guid id,
        string email,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/orders/{id}?customerEmail={Uri.EscapeDataString(email)}",
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw ApiClientException.ForOrdersResponse(response.StatusCode);
        }

        var order = await response.Content.ReadFromJsonAsync<OrderSummary>(cancellationToken);
        return order is { Id: var orderId } && orderId == id
            ? order
            : throw new InvalidDataException("The order service returned an invalid order.");
    }
}
