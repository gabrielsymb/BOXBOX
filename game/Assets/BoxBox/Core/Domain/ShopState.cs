using System;
using System.Collections.Generic;

namespace BoxBox.Domain
{
    public enum ShopPhase
    {
        PlanningOrder,
        DrivingToWholesaler,
        PickupPending,
        DrivingToStore,
        UnloadPending,
        ReadyToOpen,
        Open,
        Closed
    }

    public sealed class ShopState
    {
        private readonly ProductDefinition[] _products;
        private readonly IReadOnlyList<ProductDefinition> _productView;
        private readonly Dictionary<string, int> _productIndices;
        private readonly int[] _inventory;
        private readonly int[] _order;
        private readonly int[] _unitsSold;
        private readonly long[] _retailPrices;
        private readonly int _dailyCustomerCount;

        public ShopPhase Phase { get; internal set; }
        public Money OpeningCash { get; }
        public Money Cash { get; internal set; }
        public Money SupplierPurchases { get; internal set; }
        public Money SalesRevenue { get; internal set; }
        public int CustomersProcessed { get; internal set; }
        public int CompetitorChoices { get; internal set; }
        public int PassedCustomers { get; internal set; }
        public int DailyCustomerCount => _dailyCustomerCount;
        public int CustomersRemaining => _dailyCustomerCount - CustomersProcessed;
        public IReadOnlyList<ProductDefinition> Products => _productView;

        internal ShopState(ProductDefinition[] products, long openingCashCents, int dailyCustomerCount)
        {
            _products = products;
            _productView = Array.AsReadOnly(_products);
            _dailyCustomerCount = dailyCustomerCount;
            _productIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            _inventory = new int[products.Length];
            _order = new int[products.Length];
            _unitsSold = new int[products.Length];
            _retailPrices = new long[products.Length];
            OpeningCash = new Money(openingCashCents);
            Cash = OpeningCash;
            SupplierPurchases = Money.Zero;
            SalesRevenue = Money.Zero;
            Phase = ShopPhase.PlanningOrder;

            for (int i = 0; i < products.Length; i++)
            {
                _productIndices.Add(products[i].Id, i);
                _retailPrices[i] = products[i].StartingRetailPrice.Cents;
            }
        }

        public bool TryGetProductIndex(string productId, out int index)
        {
            if (string.IsNullOrEmpty(productId))
            {
                index = -1;
                return false;
            }

            return _productIndices.TryGetValue(productId, out index);
        }

        public int InventoryAt(int index)
        {
            return _inventory[index];
        }

        public int OrderedAt(int index)
        {
            return _order[index];
        }

        public int UnitsSoldAt(int index)
        {
            return _unitsSold[index];
        }

        public Money RetailPriceAt(int index)
        {
            return new Money(_retailPrices[index]);
        }

        internal int InventoryAtIndex(int index) => _inventory[index];
        internal int OrderAtIndex(int index) => _order[index];
        internal Money RetailPriceAtIndex(int index) => new Money(_retailPrices[index]);
        internal void SetInventory(int index, int value) => _inventory[index] = value;
        internal void SetOrder(int index, int value) => _order[index] = value;
        internal void SetUnitsSold(int index, int value) => _unitsSold[index] = value;
        internal void SetRetailPrice(int index, long value) => _retailPrices[index] = value;

        public ShopSnapshot CreateSnapshot()
        {
            var quotes = new ProductQuote[_products.Length];
            for (int i = 0; i < _products.Length; i++)
            {
                ProductDefinition product = _products[i];
                quotes[i] = new ProductQuote(
                    product.Id,
                    product.DemandWeight,
                    _inventory[i],
                    new Money(_retailPrices[i]),
                    product.CompetitorPrice,
                    product.CustomerWillingness);
            }

            return new ShopSnapshot(Array.AsReadOnly(quotes), CustomersRemaining);
        }
    }
}
