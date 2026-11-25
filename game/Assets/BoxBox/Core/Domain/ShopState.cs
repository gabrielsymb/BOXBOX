using System;
using System.Collections.Generic;

namespace BoxBox.Domain
{
    /// <summary>
    /// Where the supply run stands. The day starts parked at the store; the player can make
    /// as many wholesaler trips as they want before opening.
    /// </summary>
    public enum ShopPhase
    {
        AtStore,
        DrivingToWholesaler,
        AtWholesaler,
        DrivingToStore,
        UnloadPending,
        Open,
        Closed
    }

    public sealed class ShopState
    {
        private readonly ProductDefinition[] _products;
        private readonly IReadOnlyList<ProductDefinition> _productView;
        private readonly Dictionary<string, int> _productIndices;
        private readonly int[] _inventory;
        private readonly int[] _cargo;
        private readonly int[] _wholesalerStock;
        private readonly int[] _unitsSold;
        private readonly long[] _retailPrices;
        private readonly int _dailyCustomerCount;

        public ShopPhase Phase { get; internal set; }
        public Money OpeningCash { get; internal set; }
        /// <summary>Business day, starting at 1.</summary>
        public int Day { get; internal set; } = 1;
        public Money Cash { get; internal set; }
        public Money SupplierPurchases { get; internal set; }
        public Money SalesRevenue { get; internal set; }
        /// <summary>Wholesale cost of goods paid for but lost on the road.</summary>
        public Money LostGoods { get; internal set; }
        public int UnitsLost { get; internal set; }
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
            _cargo = new int[products.Length];
            _wholesalerStock = new int[products.Length];
            _unitsSold = new int[products.Length];
            _retailPrices = new long[products.Length];
            OpeningCash = new Money(openingCashCents);
            Cash = OpeningCash;
            SupplierPurchases = Money.Zero;
            SalesRevenue = Money.Zero;
            LostGoods = Money.Zero;
            Phase = ShopPhase.AtStore;

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

        public int InventoryAt(int index) => _inventory[index];
        /// <summary>Units bought and paid for that are on the vehicle, not yet in the store.</summary>
        public int CargoAt(int index) => _cargo[index];
        public int WholesalerStockAt(int index) => _wholesalerStock[index];
        public int UnitsSoldAt(int index) => _unitsSold[index];
        public Money RetailPriceAt(int index) => new Money(_retailPrices[index]);

        public bool HasCargo
        {
            get
            {
                foreach (int units in _cargo)
                    if (units > 0) return true;
                return false;
            }
        }

        internal void SetInventory(int index, int value) => _inventory[index] = value;
        internal void SetCargo(int index, int value) => _cargo[index] = value;
        internal void SetWholesalerStock(int index, int value) => _wholesalerStock[index] = value;
        internal void SetUnitsSold(int index, int value) => _unitsSold[index] = value;

        /// <summary>Starts the daily totals over; stock, cash and prices carry on.</summary>
        internal void ResetDailyTotals()
        {
            OpeningCash = Cash;
            SupplierPurchases = Money.Zero;
            SalesRevenue = Money.Zero;
            LostGoods = Money.Zero;
            UnitsLost = 0;
            CustomersProcessed = 0;
            CompetitorChoices = 0;
            PassedCustomers = 0;
            Array.Clear(_unitsSold, 0, _unitsSold.Length);
        }
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
