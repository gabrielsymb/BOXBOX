using Boxbox.AI.Randomness;
using Boxbox.Application.Customers;
using Boxbox.Application.Market;
using Boxbox.Domain.Catalog;
using Boxbox.Domain.Market;
using static Boxbox.Verification.MarketFixtures;
using static Boxbox.Verification.VerificationAssert;

namespace Boxbox.Verification;

internal static class AuthoritativeMarketResolutionChecks
{
    internal static void PlayerSaleCommitsWholeSnapshotOnce()
    {
        const string opportunityId = "authoritative-player-sale";
        const string decisionId = "authoritative-player-decision";
        var opportunity = StandardOpportunity(opportunityId);
        var playerStore = NewOpenStore();
        var competitorOffer = StandardCompetitorOffer("rival-rice");
        var competitor = StandardCompetitorState("rival-market", offers: [competitorOffer]);
        var initial = StandardMarketState(playerStore, [competitor], [opportunity]);

        var result = NewSharedMarketPurchaseService().Execute(
            CustomerSelecting("player-sale-customer", "purchase-at-boxbox"),
            initial,
            opportunityId,
            decisionId,
            StandardFeatures(),
            [StandardCompetitorProfile("rival-market")],
            [StandardObservation(opportunity, "rival-market", competitorOffer.OfferId)],
            new FixedRandomSource(0.5));

        Require(result.Status == SharedMarketPurchaseStatus.Resolved,
            "The player sale was not committed.");
        Require(result.PurchaseFlow?.Status == CustomerPurchaseFlowStatus.SaleCompleted,
            "The player purchase flow did not report a completed sale.");
        Require(result.Resolution?.Outcome == MarketPurchaseOutcomeKind.PlayerStoreSale,
            "The player sale produced the wrong market outcome.");
        Require(result.ResultingPlayerStoreState.CashBalanceCents == 1_500 &&
                result.ResultingPlayerStoreState.StockByProduct["rice"] == 4,
            "The player sale did not atomically credit cash and debit stock.");

        var resultingCompetitor = result.ResultingMarketState.Competitors["rival-market"];
        Require(ReferenceEquals(resultingCompetitor, competitor) &&
                resultingCompetitor.AvailableServiceCapacity == 2 &&
                resultingCompetitor.Offers.Single().AvailableQuantity == 5,
            "A player sale changed the competitor snapshot.");
        Require(result.ResultingMarketState.Opportunities[opportunityId].Status ==
                MarketOpportunityStatus.Resolved,
            "The player sale did not resolve its opportunity.");
        var resolution = result.Resolution
            ?? throw new InvalidOperationException("The player sale emitted no resolution event.");
        Require(resolution.DecisionId == decisionId &&
                resolution.CustomerId == "player-sale-customer" &&
                resolution.ProductId == "rice" &&
                resolution.Quantity == 1 &&
                resolution.OfferId is null,
            "The player sale event did not preserve its causal ids and values.");
    }

    internal static void CompetitorSaleCommitsOnlySelectedRival()
    {
        const string opportunityId = "authoritative-rival-sale";
        var opportunity = StandardOpportunity(opportunityId);
        var playerStore = NewOpenStore();
        var competitorOffer = StandardCompetitorOffer(
            "rival-rice",
            unitPriceCents: 450,
            availableQuantity: 5);
        var competitor = StandardCompetitorState(
            "rival-market",
            availableServiceCapacity: 2,
            offers: [competitorOffer]);
        var initial = StandardMarketState(playerStore, [competitor], [opportunity]);

        var result = NewSharedMarketPurchaseService().Execute(
            CustomerSelecting("rival-sale-customer", "rival-option"),
            initial,
            opportunityId,
            "authoritative-rival-decision",
            StandardFeatures(),
            [StandardCompetitorProfile("rival-market")],
            [StandardObservation(
                opportunity,
                "rival-market",
                competitorOffer.OfferId,
                observationId: "rival-option")],
            new FixedRandomSource(0.5));

        Require(result.Status == SharedMarketPurchaseStatus.Resolved &&
                result.Resolution?.Outcome == MarketPurchaseOutcomeKind.CompetitorSale,
            "The competitor choice was not committed as a competitor sale.");
        Require(ReferenceEquals(result.ResultingPlayerStoreState, playerStore) &&
                playerStore.CashBalanceCents == 1_000 &&
                playerStore.StockByProduct["rice"] == 5,
            "A competitor sale changed player cash or stock.");

        var resultingCompetitor = result.ResultingMarketState.Competitors["rival-market"];
        Require(resultingCompetitor.AvailableServiceCapacity == 1,
            "A competitor sale did not consume exactly one capacity unit.");
        Require(resultingCompetitor.Offers.Single().AvailableQuantity == 4,
            "A competitor sale did not debit exactly one offered unit.");
        Require(result.CompetitorSale?.OfferId == "rival-rice" &&
                result.CompetitorSale.UnitPriceCents == 450 &&
                result.CompetitorSale.TotalCents == 450,
            "The summarized competitor sale lost its price or offer binding.");
        Require(result.Resolution?.OfferId == "rival-rice" &&
                result.Resolution.ShopId == "rival-market",
            "The market event did not identify the selected competitor offer.");
    }

    internal static void CategoryFallbackCommitsTheActuallySelectedOffer()
    {
        const string opportunityId = "authoritative-category-fallback";
        var opportunity = StandardOpportunity(opportunityId);
        var exactOffer = StandardCompetitorOffer(
            "exact-rice",
            productId: "rice",
            availableQuantity: 0);
        var categoryOffer = StandardCompetitorOffer(
            "grocery-category",
            productId: null,
            availableQuantity: 3);
        var competitor = StandardCompetitorState(
            "rival-market",
            offers: [exactOffer, categoryOffer]);
        var initial = StandardMarketState(NewOpenStore(), [competitor], [opportunity]);

        var result = NewSharedMarketPurchaseService().Execute(
            CustomerSelecting("fallback-customer", "category-option"),
            initial,
            opportunityId,
            "fallback-decision",
            StandardFeatures(),
            [StandardCompetitorProfile("rival-market")],
            [
                StandardObservation(
                    opportunity,
                    "rival-market",
                    exactOffer.OfferId,
                    observationId: "exact-option"),
                StandardObservation(
                    opportunity,
                    "rival-market",
                    categoryOffer.OfferId,
                    observationId: "category-option")
            ],
            new FixedRandomSource(0.5));

        var offers = result.ResultingMarketState.Competitors["rival-market"].Offers
            .ToDictionary(offer => offer.OfferId, StringComparer.Ordinal);
        Require(result.Resolution?.OfferId == categoryOffer.OfferId,
            "The resolution lost the category fallback binding.");
        Require(offers[categoryOffer.OfferId].AvailableQuantity == 2,
            "The selected category fallback was not debited.");
        Require(offers[exactOffer.OfferId].AvailableQuantity == 0,
            "The invalid exact offer was changed by the fallback sale.");
    }

    internal static void NoSaleOutcomesResolveWithoutChangingAssets()
    {
        var service = NewSharedMarketPurchaseService();

        var declinedOpportunity = StandardOpportunity("authoritative-declined");
        var declinedStore = NewOpenStore();
        var declinedState = StandardMarketState(
            declinedStore,
            Array.Empty<CompetitorMarketState>(),
            [declinedOpportunity]);
        var declined = service.Execute(
            CustomerSelecting("declined-customer", "do-not-purchase"),
            declinedState,
            declinedOpportunity.OpportunityId,
            "declined-decision",
            StandardFeatures(),
            Array.Empty<CompetitorProfile>(),
            Array.Empty<CompetitorOfferObservation>(),
            new FixedRandomSource(0.5));

        Require(declined.Resolution?.Outcome == MarketPurchaseOutcomeKind.ChoseNotToPurchase &&
                ReferenceEquals(declined.ResultingPlayerStoreState, declinedStore),
            "Voluntary no-purchase changed assets or produced the wrong outcome.");

        var unservedOpportunity = StandardOpportunity("authoritative-unserved");
        var closedStore = NewStore(isOpen: false);
        var unservedState = StandardMarketState(
            closedStore,
            Array.Empty<CompetitorMarketState>(),
            [unservedOpportunity]);
        var unserved = service.Execute(
            CustomerSelecting("unserved-customer", "do-not-purchase"),
            unservedState,
            unservedOpportunity.OpportunityId,
            "unserved-decision",
            StandardFeatures(),
            Array.Empty<CompetitorProfile>(),
            Array.Empty<CompetitorOfferObservation>(),
            new FixedRandomSource(0.5));

        Require(unserved.Resolution?.Outcome == MarketPurchaseOutcomeKind.DemandUnserved &&
                ReferenceEquals(unserved.ResultingPlayerStoreState, closedStore),
            "Unserved demand changed assets or produced the wrong outcome.");
    }

    internal static void ResolvedOpportunityRejectsReplayBeforeRandomness()
    {
        const string opportunityId = "authoritative-replay-guard";
        var opportunity = StandardOpportunity(opportunityId);
        var offer = StandardCompetitorOffer("rival-rice");
        var initial = StandardMarketState(
            NewOpenStore(),
            [StandardCompetitorState("rival-market", offers: [offer])],
            [opportunity]);
        var service = NewSharedMarketPurchaseService();
        var first = service.Execute(
            CustomerSelecting("first-customer", "rival-option"),
            initial,
            opportunityId,
            "first-decision",
            StandardFeatures(),
            [StandardCompetitorProfile("rival-market")],
            [StandardObservation(
                opportunity,
                "rival-market",
                offer.OfferId,
                observationId: "rival-option")],
            new FixedRandomSource(0.5));
        var random = new CountingRandomSource(0.5);

        var replay = service.Execute(
            CustomerSelecting("replay-customer", "rival-option"),
            first.ResultingMarketState,
            opportunityId,
            "replay-decision",
            StandardFeatures(),
            [StandardCompetitorProfile("rival-market")],
            [StandardObservation(
                opportunity,
                "rival-market",
                offer.OfferId,
                observationId: "rival-option")],
            random);

        Require(replay.Status == SharedMarketPurchaseStatus.Rejected &&
                replay.RejectionReason == MarketResolutionRejectionReason.OpportunityAlreadyResolved,
            "A repeated opportunity was not rejected explicitly.");
        Require(ReferenceEquals(replay.ResultingMarketState, first.ResultingMarketState),
            "A repeated opportunity returned a different market snapshot.");
        Require(replay.PurchaseFlow is null && random.Calls == 0,
            "A repeated opportunity reached customer choice or consumed randomness.");
        Require(replay.ResultingMarketState.Competitors["rival-market"]
                    .Offers.Single().AvailableQuantity == 4,
            "A repeated opportunity debited competitor stock twice.");
    }

    internal static void SpendingLimitRejectionIsAtomic()
    {
        const string opportunityId = "over-budget";
        var playerStore = NewOpenStore();
        var rivalOffer = StandardCompetitorOffer("rival-rice", availableQuantity: 3);
        var competitor = StandardCompetitorState("rival-market", offers: [rivalOffer]);
        Require(playerStore.TryGetUnitPriceCents("rice", out var playerPrice), "Fixture needs a rice price.");
        var limit = Math.Max(playerPrice, rivalOffer.UnitPriceCents);
        Require(limit < Math.Min(playerPrice, rivalOffer.UnitPriceCents) * 2, "Fixture needs comparable prices.");
        var opportunity = new MarketPurchaseOpportunity(opportunityId, "grocery", "rice", 2, limit);
        var market = StandardMarketState(playerStore, [competitor], [opportunity]);

        var player = market.Resolve(new ResolveMarketPurchaseCommand(
            opportunityId, "over-budget-player", "buyer", MarketPurchaseOutcomeKind.PlayerStoreSale, playerStore.ShopId));
        var rival = market.Resolve(new ResolveMarketPurchaseCommand(
            opportunityId, "over-budget-rival", "buyer", MarketPurchaseOutcomeKind.CompetitorSale, competitor.CompetitorId, rivalOffer.OfferId));

        Require(player.RejectionReason == MarketResolutionRejectionReason.ExceedsSpendingLimit &&
                rival.RejectionReason == MarketResolutionRejectionReason.ExceedsSpendingLimit &&
                ReferenceEquals(player.ResultingState, market) && ReferenceEquals(rival.ResultingState, market),
            "Both stores must reject unaffordable quantities without modifying the market.");
    }

    internal static void RejectedCompetitorCommitIsAtomic()
    {
        const string opportunityId = "authoritative-stale-rival";
        var opportunity = new MarketPurchaseOpportunity(
            opportunityId,
            "grocery",
            "rice",
            quantity: 2);
        var playerStore = NewOpenStore();
        var competitor = StandardCompetitorState(
            "rival-market",
            offers: [StandardCompetitorOffer("rival-rice", availableQuantity: 1)]);
        var initial = StandardMarketState(playerStore, [competitor], [opportunity]);

        var attempt = initial.Resolve(new ResolveMarketPurchaseCommand(
            opportunityId,
            "stale-rival-decision",
            "stale-rival-customer",
            MarketPurchaseOutcomeKind.CompetitorSale,
            "rival-market",
            "rival-rice"));

        Require(!attempt.Completed &&
                attempt.RejectionReason == MarketResolutionRejectionReason.CompetitorSaleRejected &&
                attempt.CompetitorSaleRejectionReason ==
                CompetitorSaleRejectionReason.InsufficientAvailability,
            "Insufficient rival availability was not rejected with its causal reason.");
        Require(ReferenceEquals(attempt.ResultingState, initial) &&
                ReferenceEquals(attempt.ResultingState.PlayerStore, playerStore) &&
                ReferenceEquals(attempt.ResultingState.Competitors["rival-market"], competitor),
            "A rejected rival commit partially changed the market snapshot.");
        Require(initial.Opportunities[opportunityId].Status == MarketOpportunityStatus.Pending,
            "A rejected rival commit consumed the opportunity.");
    }

    internal static void DuplicateDecisionRejectsBeforeRandomness()
    {
        var firstOpportunity = StandardOpportunity("first-decision-opportunity");
        var secondOpportunity = StandardOpportunity("second-decision-opportunity");
        var initial = StandardMarketState(
            NewOpenStore(),
            Array.Empty<CompetitorMarketState>(),
            [firstOpportunity, secondOpportunity]);
        var service = NewSharedMarketPurchaseService();
        const string decisionId = "globally-unique-decision";
        var first = service.Execute(
            CustomerSelecting("first-decision-customer", "do-not-purchase"),
            initial,
            firstOpportunity.OpportunityId,
            decisionId,
            StandardFeatures(),
            Array.Empty<CompetitorProfile>(),
            Array.Empty<CompetitorOfferObservation>(),
            new FixedRandomSource(0.5));
        var random = new CountingRandomSource(0.5);

        var duplicate = service.Execute(
            CustomerSelecting("duplicate-decision-customer", "purchase-at-boxbox"),
            first.ResultingMarketState,
            secondOpportunity.OpportunityId,
            decisionId,
            StandardFeatures(),
            Array.Empty<CompetitorProfile>(),
            Array.Empty<CompetitorOfferObservation>(),
            random);

        Require(duplicate.Status == SharedMarketPurchaseStatus.Rejected &&
                duplicate.RejectionReason == MarketResolutionRejectionReason.DuplicateDecision,
            "A decision id reused by another opportunity was not rejected.");
        Require(ReferenceEquals(duplicate.ResultingMarketState, first.ResultingMarketState) &&
                duplicate.ResultingMarketState.Opportunities[secondOpportunity.OpportunityId].Status ==
                MarketOpportunityStatus.Pending,
            "A duplicate decision changed or consumed the second opportunity.");
        Require(random.Calls == 0 && duplicate.PurchaseFlow is null,
            "A duplicate decision reached customer choice or consumed randomness.");
    }

    internal static void ProductCatalogRejectsInvalidOpportunityBeforeChoice()
    {
        var catalog = new ProductCatalog(
        [
            new ProductDefinition("rice", "grocery")
        ]);

        Expect<ArgumentException>(() => _ = StandardMarketState(
            NewOpenStore(),
            Array.Empty<CompetitorMarketState>(),
            [new MarketPurchaseOpportunity(
                "invalid-product-category",
                "cosmetics",
                "rice",
                quantity: 1)],
            catalog));
        Expect<KeyNotFoundException>(() => _ = StandardMarketState(
            NewOpenStore(),
            Array.Empty<CompetitorMarketState>(),
            [new MarketPurchaseOpportunity(
                "unknown-product",
                "grocery",
                "unknown",
                quantity: 1)],
            catalog));
    }

    internal static void SeededReplayProducesTheSameResolution()
    {
        SharedMarketPurchaseResult ExecuteReplay()
        {
            const string opportunityId = "authoritative-seeded-replay";
            var opportunity = StandardOpportunity(opportunityId);
            var offer = StandardCompetitorOffer("rival-rice");
            var state = StandardMarketState(
                NewOpenStore(),
                [StandardCompetitorState("rival-market", offers: [offer])],
                [opportunity]);
            return NewSharedMarketPurchaseService().Execute(
                RegularCustomer("seeded-replay-customer"),
                state,
                opportunityId,
                "seeded-replay-decision",
                StandardFeatures(),
                [StandardCompetitorProfile("rival-market")],
                [StandardObservation(opportunity, "rival-market", offer.OfferId)],
                new SeededChoiceRandomSource(20261119));
        }

        var first = ExecuteReplay();
        var second = ExecuteReplay();

        var firstResolution = first.Resolution
            ?? throw new InvalidOperationException("The first seeded replay did not resolve.");
        var secondResolution = second.Resolution
            ?? throw new InvalidOperationException("The second seeded replay did not resolve.");
        Require(firstResolution.Outcome == secondResolution.Outcome &&
                firstResolution.ShopId == secondResolution.ShopId &&
                firstResolution.OfferId == secondResolution.OfferId &&
                firstResolution.UnitPriceCents == secondResolution.UnitPriceCents &&
                firstResolution.TotalCents == secondResolution.TotalCents,
            "The same seed and snapshot produced different causal events.");
        Require(first.ResultingPlayerStoreState.CashBalanceCents ==
                second.ResultingPlayerStoreState.CashBalanceCents &&
                first.ResultingPlayerStoreState.StockByProduct["rice"] ==
                second.ResultingPlayerStoreState.StockByProduct["rice"],
            "The same seed and snapshot produced different player state.");
        Require(first.ResultingMarketState.Competitors["rival-market"].AvailableServiceCapacity ==
                second.ResultingMarketState.Competitors["rival-market"].AvailableServiceCapacity &&
                first.ResultingMarketState.Competitors["rival-market"]
                    .Offers.Single().AvailableQuantity ==
                second.ResultingMarketState.Competitors["rival-market"]
                    .Offers.Single().AvailableQuantity,
            "The same seed and snapshot produced different competitor state.");
    }
}
