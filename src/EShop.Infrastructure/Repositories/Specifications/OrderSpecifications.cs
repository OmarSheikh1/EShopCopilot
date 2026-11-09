using EShop.ApiService.Models;
using OrderModel = EShop.ApiService.Models.Order;

namespace EShop.Infrastructure.Repositories.Specifications;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Encapsulates common order history and status query expressions.
public sealed class OrderByIdSpec : Specification<OrderModel>
{
    public OrderByIdSpec(Guid id)
    {
        Criteria = order => order.Id == id;
        AddInclude(order => order.Items);
    }
}

public sealed class AllOrdersWithItemsSpec : Specification<OrderModel>
{
    public AllOrdersWithItemsSpec() =>
        AddInclude(order => order.Items);
}

public sealed class OrderByCustomerEmailSpec : Specification<OrderModel>
{
    public OrderByCustomerEmailSpec(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var normalizedEmail = email.Trim().ToLower();
        Criteria = order => order.CustomerEmail.Trim().ToLower() == normalizedEmail;
        AddInclude(order => order.Items);
        AddOrdering(order => order.CreatedAt, ascending: false);
    }
}

public sealed class OrdersByDateRangeSpec : Specification<OrderModel>
{
    public OrdersByDateRangeSpec(DateTime? from, DateTime? to)
    {
        if (from is not null && to is not null && from > to)
        {
            throw new ArgumentException("The start date must not be after the end date.");
        }

        Criteria = order =>
            (from == null || order.CreatedAt >= from) &&
            (to == null || order.CreatedAt <= to);
        AddInclude(order => order.Items);
    }
}

public sealed class OrdersByStatusSpec : Specification<OrderModel>
{
    public OrdersByStatusSpec(OrderStatus status)
    {
        Criteria = order => order.Status == status;
        AddInclude(order => order.Items);
    }
}
