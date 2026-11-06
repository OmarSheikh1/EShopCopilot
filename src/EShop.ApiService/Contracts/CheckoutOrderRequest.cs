using System.ComponentModel.DataAnnotations;

namespace EShop.ApiService.Contracts;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Defines the client-safe checkout payload without trusted prices or product names.
/// <summary>Customer and product quantities submitted to create an order at checkout.</summary>
public sealed record CheckoutOrderRequest : IValidatableObject
{
    [Required, StringLength(200)]
    public required string CustomerName { get; init; }

    [Required, EmailAddress, StringLength(320)]
    public required string CustomerEmail { get; init; }

    [Required, MinLength(1)]
    public required List<CheckoutOrderItemRequest> Items { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Items is null)
        {
            yield break;
        }

        var productIds = new HashSet<Guid>();
        for (var index = 0; index < Items.Count; index++)
        {
            var item = Items[index];
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
                yield return new ValidationResult(
                    result.ErrorMessage,
                    result.MemberNames.Select(name => $"Items[{index}].{name}"));
            }

            if (!productIds.Add(item.ProductId))
            {
                yield return new ValidationResult(
                    "Each product may appear only once in the checkout.",
                    [$"Items[{index}].ProductId"]);
            }
        }
    }
}

/// <summary>Identifies a product and the quantity requested at checkout.</summary>
public sealed record CheckoutOrderItemRequest : IValidatableObject
{
    public required Guid ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public required int Quantity { get; init; }

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

public enum CheckoutFailure
{
    ProductNotFound,
    ProductUnavailable,
    InsufficientStock
}

public sealed record CheckoutOrderResult(OrderResponse? Order, CheckoutFailure? Failure);
