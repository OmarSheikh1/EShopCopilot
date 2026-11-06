namespace EShop.Web.Services;

// CHG-20261106-002-MY-ORDERS
// Purpose: Remembers the customer email for the current circuit so My Orders can load automatically.
public sealed class CustomerSession
{
    public string? Email { get; private set; }

    public void SetEmail(string email) => Email = email.Trim();
}
