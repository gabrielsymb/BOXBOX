namespace BoxBox.Domain
{
    public sealed class DailyReport
    {
        public Money OpeningCash { get; }
        public Money SupplierPurchases { get; }
        public Money SalesRevenue { get; }
        public Money CostOfGoodsSold { get; }
        public Money GrossProfit { get; }
        /// <summary>Wholesale cost of cargo lost on the road.</summary>
        public Money LostGoods { get; }
        public int UnitsLost { get; }
        /// <summary>Gross profit minus lost cargo.</summary>
        public Money NetProfit => GrossProfit - LostGoods;
        public Money ClosingCash { get; }
        public int UnitsSold { get; }
        public int CustomersProcessed { get; }
        public int CompetitorChoices { get; }
        public int PassedCustomers { get; }
        public int RemainingInventory { get; }

        public DailyReport(
            Money openingCash,
            Money supplierPurchases,
            Money salesRevenue,
            Money costOfGoodsSold,
            Money grossProfit,
            Money lostGoods,
            int unitsLost,
            Money closingCash,
            int unitsSold,
            int customersProcessed,
            int competitorChoices,
            int passedCustomers,
            int remainingInventory)
        {
            OpeningCash = openingCash;
            SupplierPurchases = supplierPurchases;
            SalesRevenue = salesRevenue;
            CostOfGoodsSold = costOfGoodsSold;
            GrossProfit = grossProfit;
            LostGoods = lostGoods;
            UnitsLost = unitsLost;
            ClosingCash = closingCash;
            UnitsSold = unitsSold;
            CustomersProcessed = customersProcessed;
            CompetitorChoices = competitorChoices;
            PassedCustomers = passedCustomers;
            RemainingInventory = remainingInventory;
        }
    }
}
