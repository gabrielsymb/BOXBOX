using Boxbox.AI.Contracts;
using Boxbox.Application.Customers;
using Boxbox.Application.Market;
using Boxbox.Domain.Market;
using static Boxbox.Verification.MarketFixtures;
using static Boxbox.Verification.VerificationAssert;

namespace Boxbox.Verification;

internal static class SharedMarketChecks
{
    internal static void SharedMarketHasThreeAlternatives()
    {
        var result = ExecuteMarketChoice(NewOpenStore(), RegularCustomer("customer-1"), 0.99);
        var options = result.Choice.Evaluations.Select(evaluation => evaluation.Option).ToArray();

        Require(options.Length == 3, "Expected player store, rival and no-purchase alternatives.");
        Require(options.Count(option => option.Kind == ChoiceOptionKind.DoNotPurchase) == 1,
            "Expected exactly one no-purchase alternative.");
        Require(options.Any(option => option.ShopId == "boxbox"), "BOXBOX was not offered.");
        Require(options.Any(option => option.ShopId == "rival-market"), "The rival was not offered.");
    }

    internal static void RivalChoiceDoesNotMutatePlayerStore()
    {
        var store = NewOpenStore();
        var result = ExecuteMarketChoice(store, RegularCustomer("customer-2"), 0.0);

        Require(result.Status == CustomerPurchaseFlowStatus.ChoseAlternativeStore,
            "The controlled roll should select the rival.");
        Require(result.SelectedShopId == "rival-market", "The selected rival id was not preserved.");
        Require(ReferenceEquals(store, result.ResultingStoreState),
            "A rival choice must return the unchanged player snapshot.");
        Require(result.ResultingStoreState.CashBalanceCents == 1_000, "Player cash changed after a rival choice.");
        Require(result.ResultingStoreState.StockByProduct["rice"] == 5,
            "Player stock changed after a rival choice.");
        Require(result.Sale is null, "A rival choice cannot emit a BOXBOX sale.");
    }

    internal static void ClosedPlayerStoreIsNotOffered()
    {
        var closedStore = NewStore(isOpen: false);
        var result = ExecuteMarketChoice(closedStore, RegularCustomer("customer-3"), 0.0);
        var options = result.Choice.Evaluations.Select(evaluation => evaluation.Option).ToArray();

        Require(options.All(option => option.ShopId != "boxbox"), "A closed BOXBOX must not be admissible.");
        Require(options.Any(option => option.ShopId == "rival-market"),
            "An independently admissible rival should remain available.");
        Require(result.Status == CustomerPurchaseFlowStatus.ChoseAlternativeStore,
            "The controlled roll should select the remaining rival.");
    }

    internal static void KnownPromotionChangesPreference()
    {
        var baseline = ExecuteMarketChoice(
            NewOpenStore(),
            PriceSensitiveCustomer("customer-4a"),
            0.5,
            priceAttractiveness: 400,
            decisionId: "decision-baseline");
        var promoted = ExecuteMarketChoice(
            NewOpenStore(),
            PriceSensitiveCustomer("customer-4b"),
            0.5,
            priceAttractiveness: 900,
            decisionId: "decision-promoted");

        var baselineRival = ProbabilityFor(baseline, "rival-market");
        var promotedRival = ProbabilityFor(promoted, "rival-market");
        var baselinePlayer = ProbabilityFor(baseline, "boxbox");
        var promotedPlayer = ProbabilityFor(promoted, "boxbox");

        Require(promotedRival > baselineRival, "A perceived rival promotion should raise rival preference.");
        Require(promotedPlayer < baselinePlayer,
            "Competition for one shared choice should reduce the player's relative probability.");
    }

    internal static void UnobservedRivalStateDoesNotChangePreference()
    {
        var baseline = ExecuteWithState(
            unitPriceCents: 450,
            availableQuantity: 2,
            availableServiceCapacity: 1,
            "unobserved-baseline");
        var changedPrivateState = ExecuteWithState(
            unitPriceCents: 900,
            availableQuantity: 200,
            availableServiceCapacity: 50,
            "unobserved-changed");

        Require(
            ProbabilityFor(baseline, "rival-market") == ProbabilityFor(changedPrivateState, "rival-market"),
            "State not present in the permitted observation changed rival preference.");

        CustomerPurchaseFlowResult ExecuteWithState(
            long unitPriceCents,
            int availableQuantity,
            int availableServiceCapacity,
            string opportunityId)
        {
            var opportunity = StandardOpportunity(opportunityId);
            var offer = StandardCompetitorOffer(
                "rival-rice",
                unitPriceCents: unitPriceCents,
                availableQuantity: availableQuantity);
            var competitorState = StandardCompetitorState(
                "rival-market",
                availableServiceCapacity: availableServiceCapacity,
                offers: [offer]);
            var marketState = StandardMarketState(
                NewOpenStore(),
                [competitorState],
                [opportunity]);
            return NewSharedMarketPurchaseService().Execute(
                RegularCustomer($"customer-{opportunityId}"),
                marketState,
                opportunity.OpportunityId,
                opportunity.OpportunityId,
                StandardFeatures(),
                [StandardCompetitorProfile("rival-market")],
                [StandardObservation(opportunity, "rival-market", offer.OfferId)],
                new FixedRandomSource(0.5)).PurchaseFlow
                ?? throw new InvalidOperationException("Expected a completed customer choice.");
        }
    }

    internal static void ReservedOptionIdsAreRejectedBeforeChoice()
    {
        foreach (var reservedOptionId in new[] { "purchase-at-boxbox", "do-not-purchase" })
        {
            const string decisionId = "reserved-option-decision";
            var store = NewOpenStore();
            var customer = RegularCustomer($"customer-5-{reservedOptionId}");

            Expect<ArgumentException>(() => ExecuteMarketChoice(
                store,
                customer,
                0.0,
                decisionId: decisionId,
                observationId: reservedOptionId));

            var retry = ExecuteMarketChoice(
                store,
                customer,
                0.0,
                decisionId: decisionId);
            Require(retry.Status == CustomerPurchaseFlowStatus.ChoseAlternativeStore,
                "An option-id collision consumed the decision before a valid retry.");
            Require(ReferenceEquals(store, retry.ResultingStoreState),
                "Rejecting a reserved option id changed the player snapshot.");
        }
    }

    internal static void CategoryOnlyResolutionFailsBeforeChoice()
    {
        const string opportunityId = "category-only-opportunity";
        var store = NewOpenStore();
        var customer = RegularCustomer("category-only-customer");
        var random = new CountingRandomSource(0.999999);
        var service = NewSharedMarketPurchaseService();
        var categoryOnlyOpportunity = new MarketPurchaseOpportunity(
            opportunityId,
            "grocery",
            productId: null,
            quantity: 1);
        var categoryOnlyState = StandardMarketState(
            store,
            Array.Empty<CompetitorMarketState>(),
            [categoryOnlyOpportunity]);

        var rejected = service.Execute(
            customer,
            categoryOnlyState,
            opportunityId,
            opportunityId,
            StandardFeatures(),
            Array.Empty<CompetitorProfile>(),
            Array.Empty<CompetitorOfferObservation>(),
            random);
        Require(rejected.Status == SharedMarketPurchaseStatus.Rejected &&
                rejected.RejectionReason == MarketResolutionRejectionReason.ProductRequired,
            "Category-only resolution was not rejected with the expected reason.");
        Require(random.Calls == 0, "Category-only rejection consumed randomness before choice.");

        var specificOpportunity = new MarketPurchaseOpportunity(
            opportunityId,
            "grocery",
            "rice",
            quantity: 1);
        var retryState = StandardMarketState(
            store,
            Array.Empty<CompetitorMarketState>(),
            [specificOpportunity]);
        var retry = service.Execute(
            customer,
            retryState,
            opportunityId,
            opportunityId,
            StandardFeatures(),
            Array.Empty<CompetitorProfile>(),
            Array.Empty<CompetitorOfferObservation>(),
            random);
        Require(retry.PurchaseFlow?.Status == CustomerPurchaseFlowStatus.SaleCompleted,
            "Category-only rejection consumed the decision before a valid retry.");
        Require(random.Calls == 1, "The valid retry did not consume exactly one random draw.");
    }

    internal static void UnservedDemandIsDistinctFromNoPurchase()
    {
        var service = NewSharedMarketPurchaseService();
        var emptyProfiles = Array.Empty<CompetitorProfile>();
        var emptyStates = Array.Empty<CompetitorMarketState>();
        var emptyObservations = Array.Empty<CompetitorOfferObservation>();
        var unservedOpportunity = StandardOpportunity("unserved-opportunity");
        var unservedState = StandardMarketState(
            NewStore(isOpen: false),
            emptyStates,
            [unservedOpportunity]);

        var unserved = service.Execute(
            RegularCustomer("unserved-customer"),
            unservedState,
            unservedOpportunity.OpportunityId,
            "unserved-decision",
            StandardFeatures(),
            emptyProfiles,
            emptyObservations,
            new FixedRandomSource(0.0));
        Require(unserved.PurchaseFlow?.Status == CustomerPurchaseFlowStatus.DemandUnserved,
            "Demand with no admissible shop was not classified as unserved.");

        var declinedOpportunity = StandardOpportunity("declined-opportunity");
        var declinedState = StandardMarketState(
            NewOpenStore(),
            emptyStates,
            [declinedOpportunity]);
        var declined = service.Execute(
            RegularCustomer("declining-customer"),
            declinedState,
            declinedOpportunity.OpportunityId,
            "declined-decision",
            StandardFeatures(),
            emptyProfiles,
            emptyObservations,
            new FixedRandomSource(0.0));
        Require(declined.PurchaseFlow?.Status == CustomerPurchaseFlowStatus.ChoseNotToPurchase,
            "Voluntary no-purchase with an admissible shop was not distinguished from unserved demand.");
    }
}
