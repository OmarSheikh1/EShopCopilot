using System.ComponentModel.DataAnnotations;
using EShop.ApiService.Models;

namespace EShop.ApiService.Contracts;

/// <summary>Order item data supplied when creating or replacing an order.</summary>
public sealed record OrderItemRequest : IValidatableObject
{
    public required Guid ProductId { get; init; }

    [Required, StringLength(200)]
    public required string ProductName { get; init; }

    [Range(1, int.MaxValue)]
    public required int Quantity { get; init; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public required decimal UnitPrice { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProductId == Guid.Empty)
        {
            yield return new ValidationResult(
                "ProductId must not be empty.",
                [nameof(ProductId)]);
        }
    }
}

/// <summary>Payload for creating a new order.</summary>
public sealed record CreateOrderRequest : IValidatableObject
{
    [Required, StringLength(200)]
    public required string CustomerName { get; init; }

    [Required, EmailAddress, StringLength(320)]
    public required string CustomerEmail { get; init; }

    [Required, MinLength(1)]
    public required List<OrderItemRequest> Items { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        OrderRequestValidation.ValidateItems(Items);
}

/// <summary>Payload for replacing an existing order.</summary>
public sealed record UpdateOrderRequest : IValidatableObject
{
    [Required, StringLength(200)]
    public required string CustomerName { get; init; }

    [Required, EmailAddress, StringLength(320)]
    public required string CustomerEmail { get; init; }

    [EnumDataType(typeof(OrderStatus))]
    public required OrderStatus Status { get; init; }

    [Required, MinLength(1)]
    public required List<OrderItemRequest> Items { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        OrderRequestValidation.ValidateItems(Items);
}

/// <summary>An item included in an order response.</summary>
public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>Details for an order returned by the API.</summary>
public sealed record OrderResponse(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    OrderStatus Status,
    IReadOnlyList<OrderItemResponse> Items,
    decimal TotalAmount);

internal static class OrderRequestValidation
{
    public static IEnumerable<ValidationResult> ValidateItems(
        IReadOnlyList<OrderItemRequest>? items)
    {
        if (items is null)
        {
            yield break;
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (item is null)
            {
                yield return new ValidationResult(
                    "Order items cannot be null.",
                    [$"Items[{index}]"]);
                continue;
            }

            var itemResults = new List<ValidationResult>();
            Validator.TryValidateObject(
                item,
                new ValidationContext(item),
                itemResults,
                validateAllProperties: true);

            foreach (var result in itemResults)
            {
                var memberNames = result.MemberNames
                    .Select(name => $"Items[{index}].{name}");
                yield return new ValidationResult(result.ErrorMessage, memberNames);
            }
        }
    }
}
