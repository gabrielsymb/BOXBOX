using Boxbox.AI.Contracts;
using Boxbox.AI.Customers;
using Boxbox.Application.Customers;
using Boxbox.Application.Market;
using Boxbox.Application.MiniTurn;
using Boxbox.Domain.Catalog;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;
using static Boxbox.Verification.VerificationAssert;

namespace Boxbox.Verification;

internal static class MiniTurnChecks
{
    internal static void BatchedAndIncrementalAdvanceMatch()
    {
        var batched = CreateSimulation(20261119);
        var batchedResult = batched.AdvanceTo(510);

        var incremental = CreateSimulation(20261119);
        var incrementalEvents = new List<MiniTurnEvent>();
        for (var minute = 480L; minute <= 510; minute++)
        {
            incrementalEvents.AddRange(incremental.AdvanceTo(minute).Events);
        }

        Require(
            EventFingerprint(batchedResult.Events) == EventFingerprint(incrementalEvents),
            "Mini-turn event log changed with advance batch size.");
        Require(
            StateFingerprint(batchedResult.MarketState) == StateFingerprint(incremental.MarketState),
            "Mini-turn economy changed with advance batch size.");
    }

    internal static void SeededReplayMatches()
    {
        var first = CreateSimulation(20261119).AdvanceTo(510);
        var second = CreateSimulation(20261119).AdvanceTo(510);

        Require(
            EventFingerprint(first.Events) == EventFingerprint(second.Events),
            "The same mini-turn seed did not reproduce the event log.");
        Require(
            StateFingerprint(first.MarketState) == StateFingerprint(second.MarketState),
            "The same mini-turn seed did not reproduce the economic snapshot.");
    }

    internal static void ResolvedEventsExplainCashAndStock()
    {
        const long initialCash = 2_000;
        const int initialStock = 5;
        var result = CreateSimulation(20261119).AdvanceTo(510);
        var sales = result.Events
            .Where(item => item.Kind == MiniTurnEventKind.CustomerResolved &&
                           item.Outcome == MarketPurchaseOutcomeKind.PlayerStoreSale)
            .ToArray();
        var expectedRevenue = sales.Sum(item => item.TotalCents ?? 0);
        var expectedUnits = sales.Sum(item => item.Quantity);

        Require(
            result.MarketState.PlayerStore.CashBalanceCents == initialCash + expectedRevenue,
            "Mini-turn HUD cash cannot be explained by confirmed player sales.");
        Require(
            result.MarketState.PlayerStore.StockByProduct["rice"] == initialStock - expectedUnits,
            "Mini-turn HUD stock cannot be explained by confirmed player sales.");
        Require(
            result.Events.Count(item => item.Kind == MiniTurnEventKind.CustomerArrived) == 4 &&
            result.Events.Count(item => item.Kind == MiniTurnEventKind.CustomerResolved) == 4,
            "Each planned customer must arrive and resolve exactly once.");
    }

    private static MiniTurnSimulation CreateSimulation(ulong seed)
    {
        var catalog = new ProductCatalog([new ProductDefinition("rice", "grocery")]);
        var store = new StoreState(
            "boxbox",
            isOpen: true,
            new Dictionary<string, int> { ["rice"] = 5 },
            new Dictionary<string, long> { ["rice"] = 500 },
            cashBalanceCents: 2_000);
        var competitorOffer = new CompetitorOfferState(
            "rival-rice",
            "grocery",
            "rice",
            unitPriceCents: 450,
            availableQuantity: 8,
            isActive: true);
        var competitor = new CompetitorMarketState(
            "rival-market",
            isOpen: true,
            availableServiceCapacity: 8,
            [competitorOffer]);
        var opportunities = Enumerable.Range(1, 4)
            .Select(index => new MarketPurchaseOpportunity(
                $"mini-turn-{index}",
                "grocery",
                "rice",
                quantity: 1))
            .ToArray();
        var state = new SharedMarketState(catalog, store, [competitor], opportunities);
        var profile = new CustomerPreferenceProfile("regular", 200, 150, 150, 200, 300);
        var playerFeatures = new ChoiceFeatures(650, 700, 650, 650, 500);
        var plans = opportunities.Select((opportunity, index) =>
        {
            var observation = new CompetitorOfferObservation(
                $"rival-option-{index + 1}",
                opportunity.OpportunityId,
                competitor.CompetitorId,
                competitorOffer.OfferId,
                baseUtility: 0,
                new ChoiceFeatures(700, 550, 550, 550, 500));
            return new MiniTurnCustomerPlan(
                $"customer-{index + 1}",
                opportunity.OpportunityId,
                $"decision-{index + 1}",
                arrivalMinute: 482 + index * 6,
                decisionMinute: 485 + index * 6,
                profile,
                playerFeatures,
                [observation]);
        }).ToArray();
        var competitorProfile = new CompetitorProfile(
            competitor.CompetitorId,
            "competitor.rival-market.name",
            ["grocery"],
            new CompetitorPublicAttributes("price", convenience: 550, reputation: 550));
        var service = new SharedMarketPurchaseService(
            new AdmissibleMarketOfferGenerator(),
            new CustomerPurchaseService(noPurchaseBaseUtility: 430));
        return new MiniTurnSimulation(
            seed,
            startMinute: 480,
            state,
            plans,
            [competitorProfile],
            new SoftmaxCustomerChoiceEngine(temperature: 95),
            experienceLearningRate: 180,
            service);
    }

    private static string EventFingerprint(IEnumerable<MiniTurnEvent> events) => string.Join(
        "\n",
        events.Select(item =>
            $"{item.StableId}|{item.SimulationMinute}|{item.Kind}|{item.Outcome}|" +
            $"{item.SelectedShopId}|{item.TotalCents}|{item.PlayerCashBalanceCents}|{item.PlayerRemainingStock}"));

    private static string StateFingerprint(SharedMarketState state) =>
        $"{state.PlayerStore.CashBalanceCents}|{state.PlayerStore.StockByProduct["rice"]}|" +
        string.Join(
            ",",
            state.Opportunities.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}:{pair.Value.Resolution?.Outcome}"));
}
