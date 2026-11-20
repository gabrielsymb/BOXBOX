using Boxbox.Application.BusinessDay;
using Boxbox.Domain.Catalog;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;
using static Boxbox.Verification.VerificationAssert;

namespace Boxbox.Verification;

internal static class BusinessDayChecks
{
    internal static void RejectedPurchasePreservesPreparationState()
    {
        var catalog = new ProductCatalog([new ProductDefinition("rice", "grocery")]);
        var original = new StorePreparationState(catalog, "boxbox", cashBalanceCents: 100);
        var expensive = new SupplierOffer(
            "offer-rice",
            "supplier",
            "rice",
            SupplierChannel.Regular,
            unitCostCents: 500,
            minimumQuantity: 1,
            maximumQuantity: 10);

        var result = original.Purchase(
            new PurchaseInventoryCommand("purchase-1", expensive.OfferId, 1),
            expensive);

        Require(!result.Completed, "An unaffordable purchase must be rejected.");
        Require(
            result.RejectionReason == PreparationRejectionReason.InsufficientCash,
            "The purchase rejection must explain insufficient cash.");
        Require(
            ReferenceEquals(original, result.ResultingState),
            "A rejected purchase must preserve the exact preparation snapshot.");
        Require(original.CashBalanceCents == 100 && original.StockByProduct.Count == 0,
            "A rejected purchase cannot partially change cash or stock.");
    }

    internal static void DayOneReplayIsDeterministic()
    {
        var first = GroceryDayOneScenario.Run(20261119);
        var second = GroceryDayOneScenario.Run(20261119);

        Require(
            Fingerprint(first) == Fingerprint(second),
            "The same day-one seed and decisions must reproduce the report and events.");
    }

    internal static void DayOneReportReconcilesEconomy()
    {
        var result = GroceryDayOneScenario.Run(20261119);
        var report = result.Report;
        var playerEvents = result.Events.Where(item =>
            item.Outcome == MarketPurchaseOutcomeKind.PlayerStoreSale).ToArray();

        Require(report.Status == BusinessDayStatus.Closed, "The scenario must close the daily scope.");
        Require(!result.FinalMarketState.PlayerStore.IsOpen,
            "The authoritative player store must be closed when the daily scope ends.");
        Require(report.Products.Count == 3, "The day-one report must include all three products.");
        Require(
            report.RevenueCents == playerEvents.Sum(item => item.TotalCents ?? 0),
            "Reported revenue must derive from confirmed player sales.");
        Require(
            report.ClosingCashCents == report.OpeningCashCents - report.PurchaseSpendCents + report.RevenueCents,
            "Closing cash must reconcile opening cash, purchases and confirmed revenue.");
        Require(
            report.Products.All(product =>
                product.PurchasedUnits == product.SoldUnits + product.RemainingUnits),
            "Every purchased unit must be sold or remain in inventory.");
        Require(
            report.PlayerSales + report.CompetitorSales + report.NoPurchases + report.UnservedDemand == 15,
            "Every day-one market opportunity must have one explained outcome.");
    }

    internal static void EmergencySupplyCostsMarginWithoutChangingDemand()
    {
        var regular = GroceryDayOneScenario.Run(88);
        var emergencyDecisions = GroceryDayOneScenario.DefaultDecisions
            .Select(item => item with
            {
                SupplierOfferId = $"emergency-{item.ProductId}"
            })
            .ToArray();
        var emergency = GroceryDayOneScenario.Run(88, emergencyDecisions);

        Require(
            regular.Report.PlayerSales == emergency.Report.PlayerSales &&
            regular.Report.RevenueCents == emergency.Report.RevenueCents,
            "Supplier cost is private and must not alter customer choices when prices are unchanged.");
        Require(
            emergency.Report.PurchaseSpendCents > regular.Report.PurchaseSpendCents &&
            emergency.Report.GrossResultCents < regular.Report.GrossResultCents,
            "Emergency supply must visibly cost more and reduce gross result.");
    }

    private static string Fingerprint(BusinessDayRunResult result) => string.Join(
        "|",
        result.Report.ClosingCashCents,
        result.Report.RevenueCents,
        result.Report.GrossResultCents,
        result.Report.PlayerSales,
        result.Report.CompetitorSales,
        result.Report.NoPurchases,
        string.Join(",", result.Report.Products.Select(product =>
            $"{product.ProductId}:{product.SoldUnits}:{product.RemainingUnits}:{product.InventorySignalKey}")),
        string.Join(",", result.Events.Select(item =>
            $"{item.StableId}:{item.Outcome}:{item.TotalCents}")));
}
