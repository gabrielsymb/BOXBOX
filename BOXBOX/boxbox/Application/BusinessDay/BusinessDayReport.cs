using Boxbox.Application.MiniTurn;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;

namespace Boxbox.Application.BusinessDay;

public enum BusinessDayStatus
{
    Closed
}

public sealed record ProductDayReport(
    string ProductId,
    int PurchasedUnits,
    int SoldUnits,
    int RemainingUnits,
    int CompetitorSaleUnits,
    int NoPurchaseUnits,
    long PurchaseCostCents,
    long SalesRevenueCents,
    long CostOfGoodsSoldCents,
    long GrossResultCents,
    string InventorySignalKey);

/// <summary>
/// Day-close projection derived only from confirmed purchases, market events
/// and the final authoritative store snapshot.
/// </summary>
public sealed class BusinessDayReport
{
    private BusinessDayReport(
        ulong seed,
        long openingCashCents,
        long closingCashCents,
        IReadOnlyList<ProductDayReport> products,
        int playerSales,
        int competitorSales,
        int noPurchases,
        int unservedDemand)
    {
        Seed = seed;
        Status = BusinessDayStatus.Closed;
        OpeningCashCents = openingCashCents;
        ClosingCashCents = closingCashCents;
        Products = products;
        PlayerSales = playerSales;
        CompetitorSales = competitorSales;
        NoPurchases = noPurchases;
        UnservedDemand = unservedDemand;
    }

    public ulong Seed { get; }
    public BusinessDayStatus Status { get; }
    public long OpeningCashCents { get; }
    public long ClosingCashCents { get; }
    public IReadOnlyList<ProductDayReport> Products { get; }
    public int PlayerSales { get; }
    public int CompetitorSales { get; }
    public int NoPurchases { get; }
    public int UnservedDemand { get; }
    public long PurchaseSpendCents => Products.Sum(product => product.PurchaseCostCents);
    public long RevenueCents => Products.Sum(product => product.SalesRevenueCents);
    public long CostOfGoodsSoldCents => Products.Sum(product => product.CostOfGoodsSoldCents);
    public long GrossResultCents => RevenueCents - CostOfGoodsSoldCents;
    public long InventoryAtCostCents => PurchaseSpendCents - CostOfGoodsSoldCents;

    public static BusinessDayReport Create(
        ulong seed,
        long openingCashCents,
        IReadOnlyCollection<CompletedInventoryPurchase> purchases,
        IReadOnlyCollection<MiniTurnEvent> events,
        SharedMarketState finalMarketState)
    {
        ArgumentNullException.ThrowIfNull(purchases);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(finalMarketState);

        var resolved = events
            .Where(item => item.Kind == MiniTurnEventKind.CustomerResolved)
            .ToArray();
        var products = purchases
            .Select(item => item.ProductId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .Select(productId => CreateProductReport(
                productId,
                purchases.Where(item => string.Equals(
                    item.ProductId,
                    productId,
                    StringComparison.Ordinal)).ToArray(),
                resolved.Where(item => string.Equals(
                    item.ProductId,
                    productId,
                    StringComparison.Ordinal)).ToArray(),
                finalMarketState.PlayerStore.StockByProduct.GetValueOrDefault(productId)))
            .ToArray();

        return new BusinessDayReport(
            seed,
            openingCashCents,
            finalMarketState.PlayerStore.CashBalanceCents,
            Array.AsReadOnly(products),
            resolved.Count(item => item.Outcome == MarketPurchaseOutcomeKind.PlayerStoreSale),
            resolved.Count(item => item.Outcome == MarketPurchaseOutcomeKind.CompetitorSale),
            resolved.Count(item => item.Outcome == MarketPurchaseOutcomeKind.ChoseNotToPurchase),
            resolved.Count(item => item.Outcome == MarketPurchaseOutcomeKind.DemandUnserved));
    }

    private static ProductDayReport CreateProductReport(
        string productId,
        IReadOnlyList<CompletedInventoryPurchase> purchases,
        IReadOnlyCollection<MiniTurnEvent> events,
        int remainingUnits)
    {
        var purchasedUnits = purchases.Sum(item => item.Quantity);
        var soldEvents = events
            .Where(item => item.Outcome == MarketPurchaseOutcomeKind.PlayerStoreSale)
            .ToArray();
        var soldUnits = soldEvents.Sum(item => item.Quantity);
        var revenueCents = soldEvents.Sum(item => item.TotalCents ?? 0);
        var costOfGoodsSoldCents = CalculateFifoCost(purchases, soldUnits);
        var competitorUnits = events
            .Where(item => item.Outcome == MarketPurchaseOutcomeKind.CompetitorSale)
            .Sum(item => item.Quantity);
        var noPurchaseUnits = events
            .Where(item => item.Outcome is MarketPurchaseOutcomeKind.ChoseNotToPurchase or
                MarketPurchaseOutcomeKind.DemandUnserved)
            .Sum(item => item.Quantity);
        var signalKey = DetermineInventorySignal(purchasedUnits, soldUnits, remainingUnits);

        return new ProductDayReport(
            productId,
            purchasedUnits,
            soldUnits,
            remainingUnits,
            competitorUnits,
            noPurchaseUnits,
            purchases.Sum(item => item.TotalCostCents),
            revenueCents,
            costOfGoodsSoldCents,
            revenueCents - costOfGoodsSoldCents,
            signalKey);
    }

    private static long CalculateFifoCost(
        IReadOnlyList<CompletedInventoryPurchase> purchases,
        int soldUnits)
    {
        var remainingToAllocate = soldUnits;
        var total = 0L;
        foreach (var purchase in purchases)
        {
            var units = Math.Min(remainingToAllocate, purchase.Quantity);
            total = checked(total + units * purchase.UnitCostCents);
            remainingToAllocate -= units;
            if (remainingToAllocate == 0)
            {
                break;
            }
        }

        if (remainingToAllocate != 0)
        {
            throw new InvalidOperationException(
                $"Confirmed sales exceed purchased inventory for '{purchases.FirstOrDefault()?.ProductId}'.");
        }

        return total;
    }

    private static string DetermineInventorySignal(
        int purchasedUnits,
        int soldUnits,
        int remainingUnits)
    {
        if (remainingUnits == 0 && soldUnits > 0)
        {
            return "report.inventory.sold_out";
        }

        if (soldUnits == 0)
        {
            return "report.inventory.no_sales";
        }

        return (soldUnits * 4) switch
        {
            var value when value >= purchasedUnits * 3 => "report.inventory.high_turnover",
            var value when value <= purchasedUnits => "report.inventory.slow_moving",
            _ => "report.inventory.balanced"
        };
    }
}
