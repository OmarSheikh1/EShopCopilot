using EShop.Web.Contracts;
using EShop.Web.Services;
using Xunit;

namespace EShop.Web.Tests;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Verifies cart merging, totals, notifications, and circuit isolation.
public sealed class CartStateTests
{
    [Fact]
    public void Add_MergesLinesAndNotifiesSubscribers()
    {
        var cart = new CartState();
        var productId = Guid.NewGuid();
        var notifications = 0;
        cart.Changed += () => notifications++;

        cart.Add(productId);
        cart.Add(productId, 2);

        var snapshot = cart.GetSnapshot();
        var line = Assert.Single(snapshot.Items);
        Assert.Equal(3, line.Quantity);
        Assert.Equal(3, cart.ItemCount);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public void GetTotal_UsesCurrentProductPrice()
    {
        var cart = new CartState();
        var productId = Guid.NewGuid();
        cart.Add(productId, 2);

        var total = cart.GetTotal(
        [
            new ProductSummary(productId, "Mouse", null, "M-1", 10.25m, 5, "Accessories", true)
        ]);

        Assert.Equal(20.50m, total);
    }

    [Fact]
    public void TryClear_DoesNotRemoveItemsChangedAfterCheckoutSnapshot()
    {
        var cart = new CartState();
        cart.Add(Guid.NewGuid());
        var checkoutSnapshot = cart.GetSnapshot();
        cart.Add(Guid.NewGuid());

        Assert.False(cart.TryClear(checkoutSnapshot.Version));
        Assert.Equal(2, cart.ItemCount);
    }

    [Fact]
    public void SeparateInstances_KeepIndependentCarts()
    {
        var firstCart = new CartState();
        var secondCart = new CartState();

        firstCart.Add(Guid.NewGuid());

        Assert.Equal(1, firstCart.ItemCount);
        Assert.Equal(0, secondCart.ItemCount);
    }

    [Fact]
    public void SetQuantityAndRemove_UpdateCartContents()
    {
        var cart = new CartState();
        var productId = Guid.NewGuid();
        cart.Add(productId);

        Assert.True(cart.SetQuantity(productId, 4));
        Assert.Equal(4, Assert.Single(cart.GetSnapshot().Items).Quantity);
        Assert.True(cart.Remove(productId));
        Assert.Equal(0, cart.ItemCount);
    }

    [Fact]
    public void Clear_RemovesAllLines()
    {
        var cart = new CartState();
        cart.Add(Guid.NewGuid());
        cart.Add(Guid.NewGuid());

        cart.Clear();

        Assert.Empty(cart.GetSnapshot().Items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Add_RejectsNonPositiveQuantities(int quantity)
    {
        var cart = new CartState();

        Assert.Throws<ArgumentOutOfRangeException>(() => cart.Add(Guid.NewGuid(), quantity));
        Assert.Equal(0, cart.ItemCount);
    }
}
