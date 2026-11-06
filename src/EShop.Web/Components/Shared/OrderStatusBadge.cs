namespace EShop.Web.Components.Shared;

// CHG-20261106-002-MY-ORDERS
// Purpose: Maps order status names to Bootstrap badge styles.
public static class OrderStatusBadge
{
    public static string CssClass(string status) =>
        status switch
        {
            "Confirmed" => "text-bg-primary",
            "Shipped" => "text-bg-info",
            "Delivered" => "text-bg-success",
            "Cancelled" => "text-bg-danger",
            _ => "text-bg-secondary"
        };
}
