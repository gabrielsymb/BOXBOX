using Boxbox.AI.Contracts;
using Boxbox.AI.Customers;
using Boxbox.Application.Customers;
using Boxbox.Application.Market;
using Boxbox.Application.MiniTurn;
using Boxbox.Domain.Catalog;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;

namespace Boxbox.Application.BusinessDay;

public sealed record DayOneProductDecision(
    string ProductId,
    string SupplierOfferId,
    int OrderQuantity,
    long SalePriceCents);

public sealed record BusinessDayRunResult(
    BusinessDayReport Report,
    IReadOnlyList<MiniTurnEvent> Events,
    SharedMarketState FinalMarketState);

/// <summary>
/// Small content-backed experiment for the first complete grocery day. Values
/// are prototype hypotheses and deliberately live outside the reusable domain.
/// </summary>
public static class GroceryDayOneScenario
{
    public const long OpeningMinute = 480;
    public const long ClosingMinute = 1080;
    public const long OpeningCashCents = 20_000;

    private static readonly ProductDefinition[] Products =
    [
        new("rice", "grocery"),
        new("soda", "grocery"),
        new("milk", "grocery")
    ];

    private static readonly SupplierOffer[] Offers =
    [
        new("regular-rice", "supplier.neighborhood_wholesale", "rice", SupplierChannel.Regular, 500, 1, 20),
        new("emergency-rice", "supplier.emergency", "rice", SupplierChannel.Emergency, 650, 1, 10),
        new("regular-soda", "supplier.neighborhood_wholesale", "soda", SupplierChannel.Regular, 250, 1, 24),
        new("emergency-soda", "supplier.emergency", "soda", SupplierChannel.Emergency, 340, 1, 12),
        new("regular-milk", "supplier.neighborhood_wholesale", "milk", SupplierChannel.Regular, 400, 1, 16),
        new("emergency-milk", "supplier.emergency", "milk", SupplierChannel.Emergency, 520, 1, 8)
    ];

    private static readonly IReadOnlyDictionary<string, long> CompetitorPrices =
        new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["rice"] = 750,
            ["soda"] = 480,
            ["milk"] = 620
        };

    private static readonly IReadOnlyDictionary<string, long[]> ArrivalMinutes =
        new Dictionary<string, long[]>(StringComparer.Ordinal)
        {
            ["rice"] = [500, 620, 740, 860, 980],
            ["soda"] = [540, 660, 780, 900, 1020],
            ["milk"] = [570, 690, 810, 930, 1050]
        };

    public static IReadOnlyList<DayOneProductDecision> DefaultDecisions { get; } =
        Array.AsReadOnly<DayOneProductDecision>(
        [
            new("rice", "regular-rice", 5, 800),
            new("soda", "regular-soda", 6, 450),
            new("milk", "regular-milk", 4, 650)
        ]);

    public static BusinessDayRunResult Run(
        ulong seed,
        IEnumerable<DayOneProductDecision>? decisions = null)
    {
        var selectedDecisions = (decisions ?? DefaultDecisions).ToArray();
        ValidateDecisions(selectedDecisions);
        var catalog = new ProductCatalog(Products);
        var preparation = PrepareStore(catalog, selectedDecisions);
        var opened = preparation.OpenStore();
        if (!opened.Completed || opened.Completion is null)
        {
            throw new InvalidOperationException(
                $"Day-one store could not open: {opened.RejectionReason}.");
        }

        var opportunities = CreateOpportunities();
        var competitorOffers = Products.Select(product => new CompetitorOfferState(
            $"rival-{product.ProductId}",
            product.CategoryId,
            product.ProductId,
            CompetitorPrices[product.ProductId],
            availableQuantity: 20,
            isActive: true)).ToArray();
        var competitor = new CompetitorMarketState(
            "rival-market",
            isOpen: true,
            availableServiceCapacity: opportunities.Length,
            competitorOffers);
        var market = new SharedMarketState(
            catalog,
            opened.Completion,
            [competitor],
            opportunities);
        var plans = CreateCustomerPlans(selectedDecisions, opportunities);
        var competitorProfile = new CompetitorProfile(
            competitor.CompetitorId,
            "competitor.rival-market.name",
            ["grocery"],
            new CompetitorPublicAttributes("price", convenience: 560, reputation: 540));
        var service = new SharedMarketPurchaseService(
            new AdmissibleMarketOfferGenerator(),
            new CustomerPurchaseService(noPurchaseBaseUtility: 430));
        var simulation = new MiniTurnSimulation(
            seed,
            OpeningMinute,
            market,
            plans,
            [competitorProfile],
            new SoftmaxCustomerChoiceEngine(temperature: 95),
            experienceLearningRate: 160,
            service);
        var advance = simulation.AdvanceTo(ClosingMinute);
        var closedMarketState = advance.MarketState.ClosePlayerStore();
        var report = BusinessDayReport.Create(
            seed,
            OpeningCashCents,
            preparation.Purchases,
            advance.Events,
            closedMarketState);

        return new BusinessDayRunResult(report, advance.Events, closedMarketState);
    }

    private static StorePreparationState PrepareStore(
        ProductCatalog catalog,
        IReadOnlyCollection<DayOneProductDecision> decisions)
    {
        var state = new StorePreparationState(catalog, "boxbox", OpeningCashCents);
        foreach (var decision in decisions.OrderBy(item => item.ProductId, StringComparer.Ordinal))
        {
            var offer = Offers.Single(item => string.Equals(
                item.OfferId,
                decision.SupplierOfferId,
                StringComparison.Ordinal));
            var purchase = state.Purchase(
                new PurchaseInventoryCommand(
                    $"day-one/purchase/{decision.ProductId}",
                    offer.OfferId,
                    decision.OrderQuantity),
                offer);
            if (!purchase.Completed)
            {
                throw new InvalidOperationException(
                    $"Day-one purchase rejected for '{decision.ProductId}': {purchase.RejectionReason}.");
            }

            state = purchase.ResultingState;
            var price = state.SetPrice(new SetProductPriceCommand(
                $"day-one/price/{decision.ProductId}",
                decision.ProductId,
                decision.SalePriceCents));
            if (!price.Completed)
            {
                throw new InvalidOperationException(
                    $"Day-one price rejected for '{decision.ProductId}': {price.RejectionReason}.");
            }

            state = price.ResultingState;
        }

        return state;
    }

    private static MarketPurchaseOpportunity[] CreateOpportunities() => ArrivalMinutes
        .OrderBy(pair => pair.Key, StringComparer.Ordinal)
        .SelectMany(pair => pair.Value.Select((_, index) => new MarketPurchaseOpportunity(
            $"day-one/{pair.Key}/{index + 1}",
            "grocery",
            pair.Key,
            quantity: 1)))
        .ToArray();

    private static MiniTurnCustomerPlan[] CreateCustomerPlans(
        IReadOnlyCollection<DayOneProductDecision> decisions,
        IReadOnlyCollection<MarketPurchaseOpportunity> opportunities)
    {
        var priceByProduct = decisions.ToDictionary(
            item => item.ProductId,
            item => item.SalePriceCents,
            StringComparer.Ordinal);
        var opportunityById = opportunities.ToDictionary(
            item => item.OpportunityId,
            StringComparer.Ordinal);
        var budgetProfile = new CustomerPreferenceProfile("budget", 420, 120, 160, 120, 180);
        var regularProfile = new CustomerPreferenceProfile("regular", 260, 160, 180, 180, 220);
        var sequence = 0;

        return ArrivalMinutes
            .SelectMany(pair => pair.Value.Select((minute, index) =>
            {
                sequence++;
                var productId = pair.Key;
                var opportunityId = $"day-one/{productId}/{index + 1}";
                var opportunity = opportunityById[opportunityId];
                var playerPrice = priceByProduct[productId];
                var rivalPrice = CompetitorPrices[productId];
                var playerFeatures = new ChoiceFeatures(
                    PriceAttractiveness(playerPrice, rivalPrice),
                    QueueComfort: 620,
                    Convenience: 650,
                    Reputation: 560,
                    Familiarity: 520);
                var rivalFeatures = new ChoiceFeatures(
                    PriceAttractiveness(rivalPrice, playerPrice),
                    QueueComfort: 570,
                    Convenience: 560,
                    Reputation: 540,
                    Familiarity: 600);
                var observation = new CompetitorOfferObservation(
                    $"rival-observation/{productId}/{index + 1}",
                    opportunity.OpportunityId,
                    "rival-market",
                    $"rival-{productId}",
                    baseUtility: 0,
                    rivalFeatures);

                return new MiniTurnCustomerPlan(
                    $"customer-{sequence}",
                    opportunity.OpportunityId,
                    $"decision-{sequence}",
                    minute,
                    minute + 3,
                    sequence % 2 == 0 ? budgetProfile : regularProfile,
                    playerFeatures,
                    [observation]);
            }))
            .OrderBy(plan => plan.ArrivalMinute)
            .ThenBy(plan => plan.OpportunityId, StringComparer.Ordinal)
            .ToArray();
    }

    private static int PriceAttractiveness(long ownPriceCents, long comparisonPriceCents)
    {
        var ratio = (comparisonPriceCents * 500L) / ownPriceCents;
        return (int)Math.Clamp(ratio, 0, 1000);
    }

    private static void ValidateDecisions(IReadOnlyCollection<DayOneProductDecision> decisions)
    {
        if (decisions.Count != Products.Length ||
            decisions.Select(item => item.ProductId).Distinct(StringComparer.Ordinal).Count() != Products.Length ||
            Products.Any(product => decisions.All(item => !string.Equals(
                item.ProductId,
                product.ProductId,
                StringComparison.Ordinal))))
        {
            throw new ArgumentException(
                "Day one requires exactly one decision for each prototype product.",
                nameof(decisions));
        }

        foreach (var decision in decisions)
        {
            var offer = Offers.SingleOrDefault(item => string.Equals(
                item.OfferId,
                decision.SupplierOfferId,
                StringComparison.Ordinal));
            if (offer is null || !string.Equals(
                    offer.ProductId,
                    decision.ProductId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Supplier offer does not match product '{decision.ProductId}'.",
                    nameof(decisions));
            }
        }
    }
}
