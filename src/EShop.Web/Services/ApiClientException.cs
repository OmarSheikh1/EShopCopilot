using System.Net;

namespace EShop.Web.Services;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Converts API status codes to safe, user-facing storefront messages.
public sealed class ApiClientException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public static ApiClientException ForProductResponse(HttpStatusCode statusCode) =>
        new(statusCode, "The product catalog could not be loaded. Please try again.");

    public static ApiClientException ForOrdersResponse(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.BadRequest => new(statusCode, "Enter a valid email address."),
            HttpStatusCode.NotFound => new(statusCode, "Order not found for this email."),
            _ => new(statusCode, "Your orders could not be loaded. Please try again.")
        };

    public static ApiClientException ForCheckoutResponse(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.BadRequest =>
                new(statusCode, "Checkout details or cart items are invalid. Please review and try again."),
            HttpStatusCode.NotFound =>
                new(statusCode, "A product in your cart could not be found. Remove it and try again."),
            HttpStatusCode.Conflict =>
                new(statusCode, "Product availability changed. Adjust your cart and try again."),
            _ => new(statusCode, "Checkout could not be completed. Your cart has been kept.")
        };
}
