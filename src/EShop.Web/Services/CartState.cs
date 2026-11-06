using EShop.Web.Contracts;

namespace EShop.Web.Services;

// CHG-20261106-001-SHOPPING-CART
// Purpose: Holds one isolated, in-memory shopping cart per Blazor circuit.
public sealed class CartState
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, int> _quantities = [];
    private long _version;

    public event Action? Changed;

    public int ItemCount
    {
        get
        {
            lock (_sync)
            {
                return _quantities.Values.Sum();
            }
        }
    }

    public CartSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new CartSnapshot(
                _version,
                _quantities.Select(pair => new CartLine(pair.Key, pair.Value)).ToArray());
        }
    }

    public decimal GetTotal(IEnumerable<ProductSummary> products)
    {
        ArgumentNullException.ThrowIfNull(products);

        var productPrices = products
            .Where(product => product is not null)
            .ToDictionary(product => product.Id, product => product.Price);
        var snapshot = GetSnapshot();

        return snapshot.Items
            .Where(item => productPrices.ContainsKey(item.ProductId))
            .Sum(item => productPrices[item.ProductId] * item.Quantity);
    }

    public void Add(Guid productId, int quantity = 1)
    {
        ValidateProductAndQuantity(productId, quantity);

        lock (_sync)
        {
            _quantities[productId] = checked(_quantities.GetValueOrDefault(productId) + quantity);
            _version++;
        }

        Changed?.Invoke();
    }

    public bool SetQuantity(Guid productId, int quantity)
    {
        ValidateProductAndQuantity(productId, quantity);
        bool updated;

        lock (_sync)
        {
            updated = _quantities.ContainsKey(productId);
            if (updated)
            {
                _quantities[productId] = quantity;
                _version++;
            }
        }

        if (updated)
        {
            Changed?.Invoke();
        }

        return updated;
    }

    public bool Remove(Guid productId)
    {
        bool removed;
        lock (_sync)
        {
            removed = _quantities.Remove(productId);
            if (removed)
            {
                _version++;
            }
        }

        if (removed)
        {
            Changed?.Invoke();
        }

        return removed;
    }

    public void Clear()
    {
        lock (_sync)
        {
            if (_quantities.Count == 0)
            {
                return;
            }

            _quantities.Clear();
            _version++;
        }

        Changed?.Invoke();
    }

    public bool TryClear(long expectedVersion)
    {
        lock (_sync)
        {
            if (_version != expectedVersion)
            {
                return false;
            }

            if (_quantities.Count == 0)
            {
                return true;
            }

            _quantities.Clear();
            _version++;
        }

        Changed?.Invoke();
        return true;
    }

    private static void ValidateProductAndQuantity(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("A product ID is required.", nameof(productId));
        }

        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }
    }
}
