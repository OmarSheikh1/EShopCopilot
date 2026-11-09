using EShop.ApiService.Contracts;
using EShop.ApiService.Models;
using EShop.Infrastructure.Repositories;

namespace EShop.ApiService.Services;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Implements customer order operations and checkout over persistent repositories.
public sealed class OrderService(IUnitOfWork unitOfWork) : IOrderService
{
    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(
        CancellationToken cancellationToken) =>
        (await unitOfWork.Orders.GetAllWithItemsAsync(cancellationToken))
        .Select(ToResponse)
        .ToArray();

    public async Task<OrderResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdWithItemsAsync(id, cancellationToken);
        return order is null ? null : ToResponse(order);
    }

    // CHG-20261106-002-MY-ORDERS
    // Change: Keeps customer matching case-insensitive while querying the database.
    public async Task<IReadOnlyList<OrderResponse>> GetByCustomerEmailAsync(
        string email,
        CancellationToken cancellationToken) =>
        (await unitOfWork.Orders.GetByCustomerEmailAsync(email, cancellationToken))
        .Select(ToResponse)
        .ToArray();

    // CHG-20261106-002-MY-ORDERS
    // Change: Preserves ID-and-email ownership matching after persistence is introduced.
    public async Task<OrderResponse?> GetByIdAndEmailAsync(
        Guid id,
        string email,
        CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdWithItemsAsync(id, cancellationToken);
        return order is not null && EmailMatches(order, email)
            ? ToResponse(order)
            : null;
    }

    public async Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = CreateOrder(
            request.CustomerName,
            request.CustomerEmail,
            request.Items);
        await unitOfWork.Orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(order);
    }

    // CHG-20261106-001-SHOPPING-CART
    // Change: Persists a checkout only after all current product data has been validated.
    public async Task<CheckoutOrderResult> CheckoutAsync(
        CheckoutOrderRequest request,
        CancellationToken cancellationToken)
    {
        var orderItems = new List<OrderItemRequest>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var product = await unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null)
            {
                return new CheckoutOrderResult(null, CheckoutFailure.ProductNotFound);
            }

            if (!product.IsActive)
            {
                return new CheckoutOrderResult(null, CheckoutFailure.ProductUnavailable);
            }

            if (item.Quantity > product.StockQuantity)
            {
                return new CheckoutOrderResult(null, CheckoutFailure.InsufficientStock);
            }

            orderItems.Add(new OrderItemRequest
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        var order = CreateOrder(
            request.CustomerName,
            request.CustomerEmail,
            orderItems);
        await unitOfWork.Orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CheckoutOrderResult(ToResponse(order), null);
    }

    // CHG-20261106-003-DATABASE-INFRASTRUCTURE
    // Change: Reconciles tracked line items without replacing the order aggregate.
    public async Task<OrderResponse?> UpdateAsync(
        Guid id,
        UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdWithItemsAsync(id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        order.CustomerName = request.CustomerName;
        order.CustomerEmail = request.CustomerEmail;
        order.Status = request.Status;
        var existingItems = order.Items.ToArray();
        var retainedItemCount = Math.Min(existingItems.Length, request.Items.Count);
        for (var index = 0; index < retainedItemCount; index++)
        {
            CopyItemValues(request.Items[index], existingItems[index]);
        }

        for (var index = retainedItemCount; index < existingItems.Length; index++)
        {
            unitOfWork.OrderItems.Delete(existingItems[index]);
            order.Items.Remove(existingItems[index]);
        }

        for (var index = retainedItemCount; index < request.Items.Count; index++)
        {
            order.Items.Add(ToModel(request.Items[index], order));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(order);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.Orders.DeleteAsync(id, cancellationToken))
        {
            return false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Order CreateOrder(
        string customerName,
        string customerEmail,
        IReadOnlyList<OrderItemRequest> items)
    {
        var order = new Order
        {
            CustomerName = customerName,
            CustomerEmail = customerEmail
        };
        order.Items = items.Select(item => ToModel(item, order)).ToList();
        return order;
    }

    private static OrderItem ToModel(OrderItemRequest item, Order order) =>
        new()
        {
            Order = order,
            OrderId = order.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        };

    private static void CopyItemValues(OrderItemRequest source, OrderItem destination)
    {
        destination.ProductId = source.ProductId;
        destination.ProductName = source.ProductName;
        destination.Quantity = source.Quantity;
        destination.UnitPrice = source.UnitPrice;
    }

    private static bool EmailMatches(Order order, string email) =>
        string.Equals(order.CustomerEmail.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase);

    private static OrderResponse ToResponse(Order order) =>
        new(
            order.Id,
            order.CustomerName,
            order.CustomerEmail,
            ToDateTimeOffset(order.CreatedAt),
            order.UpdatedAt is { } updatedAt ? ToDateTimeOffset(updatedAt) : null,
            order.Status,
            order.Items.Select(item => new OrderItemResponse(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal)).ToArray(),
            order.TotalAmount);

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(value),
            DateTimeKind.Local => new DateTimeOffset(value),
            _ => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
        };
}
