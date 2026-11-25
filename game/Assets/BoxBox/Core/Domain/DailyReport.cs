namespace BoxBox.Domain
{
    public sealed class DailyReport
    {
        public Money OpeningCash { get; }
        public Money SupplierPurchases { get; }
        public Money SalesRevenue { get; }
        public Money CostOfGoodsSold { get; }
        public Money GrossProfit { get; }
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
            ClosingCash = closingCash;
            UnitsSold = unitsSold;
            CustomersProcessed = customersProcessed;
            CompetitorChoices = competitorChoices;
            PassedCustomers = passedCustomers;
            RemainingInventory = remainingInventory;
        }
    }
}
