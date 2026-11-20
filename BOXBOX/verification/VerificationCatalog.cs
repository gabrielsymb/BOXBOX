namespace Boxbox.Verification;

internal readonly record struct VerificationCheck(string Name, Action Run);

internal static class VerificationCatalog
{
    internal static IReadOnlyList<VerificationCheck> Checks { get; } =
    [
        new("north/south and east/west traffic use separate lanes", TrafficLaneGridChecks.OppositeFlowsUseSeparateLanes),
        new("road maneuvers target junctions ahead of moving cars", TrafficLaneGridChecks.EveryManeuverTargetsAJunctionAhead),
        new("route generation skips the current junction center", TrafficLaneGridChecks.JunctionCenterDoesNotRetargetItself),
        new("intersection spawn exclusions work across negative coordinates", TrafficLaneGridChecks.SpawnExclusionAccountsForNegativeCoordinates),
        new("shared market exposes BOXBOX, rival and no-purchase", SharedMarketChecks.SharedMarketHasThreeAlternatives),
        new("choosing a rival does not mutate player stock or cash", SharedMarketChecks.RivalChoiceDoesNotMutatePlayerStore),
        new("closed BOXBOX is absent while an admissible rival remains", SharedMarketChecks.ClosedPlayerStoreIsNotOffered),
        new("a known promotion increases only the perceived rival preference", SharedMarketChecks.KnownPromotionChangesPreference),
        new("unobserved rival state does not change customer preference", SharedMarketChecks.UnobservedRivalStateDoesNotChangePreference),
        new("reserved market option ids are rejected before choice", SharedMarketChecks.ReservedOptionIdsAreRejectedBeforeChoice),
        new("a mismatched proposal decision id is rejected", ChoiceProposalChecks.MismatchedDecisionIdIsRejected),
        new("proposal evaluations must use the exact admissible id set", ChoiceProposalChecks.InvalidEvaluationSetsAreRejected),
        new("a proposal cannot select an unknown option id", ChoiceProposalChecks.UnknownSelectedOptionIsRejected),
        new("a proposal cannot select a zero-probability option", ChoiceProposalChecks.ZeroProbabilitySelectionIsRejected),
        new("proposal probabilities must be finite and bounded", ChoiceProposalChecks.InvalidProbabilitiesAreRejected),
        new("proposal probabilities must sum to one", ChoiceProposalChecks.InvalidProbabilitySumIsRejected),
        new("every competitor passability guard filters only invalid offers", MarketOfferPassabilityChecks.PassabilityGuardsFilterInvalidOffers),
        new("a valid category offer can replace an invalid exact offer", MarketOfferPassabilityChecks.CategoryOfferFallsBackFromInvalidExactOffer),
        new("a valid exact offer takes precedence over its category offer", MarketOfferPassabilityChecks.ExactOfferTakesPrecedence),
        new("market alternatives have canonical order independent of input order", MarketOfferPassabilityChecks.MarketOfferOrderIsCanonical),
        new("category-only resolution fails before consuming the decision", SharedMarketChecks.CategoryOnlyResolutionFailsBeforeChoice),
        new("unserved demand is distinct from voluntary no-purchase", SharedMarketChecks.UnservedDemandIsDistinctFromNoPurchase),
        new("a BOXBOX sale commits cash, stock and opportunity exactly once", AuthoritativeMarketResolutionChecks.PlayerSaleCommitsWholeSnapshotOnce),
        new("a rival sale changes only the selected summarized competitor", AuthoritativeMarketResolutionChecks.CompetitorSaleCommitsOnlySelectedRival),
        new("category fallback commits the exact offer admitted by passability", AuthoritativeMarketResolutionChecks.CategoryFallbackCommitsTheActuallySelectedOffer),
        new("no-sale outcomes resolve without changing economic assets", AuthoritativeMarketResolutionChecks.NoSaleOutcomesResolveWithoutChangingAssets),
        new("a resolved opportunity rejects replay before randomness", AuthoritativeMarketResolutionChecks.ResolvedOpportunityRejectsReplayBeforeRandomness),
        new("a rejected competitor commit preserves the entire snapshot", AuthoritativeMarketResolutionChecks.RejectedCompetitorCommitIsAtomic),
        new("a duplicate decision is rejected before randomness", AuthoritativeMarketResolutionChecks.DuplicateDecisionRejectsBeforeRandomness),
        new("the product catalog rejects invalid opportunity bindings", AuthoritativeMarketResolutionChecks.ProductCatalogRejectsInvalidOpportunityBeforeChoice),
        new("the same seed and snapshot reproduce the market resolution", AuthoritativeMarketResolutionChecks.SeededReplayProducesTheSameResolution),
        new("mini-turn batching does not change events or economy", MiniTurnChecks.BatchedAndIncrementalAdvanceMatch),
        new("mini-turn seeded replay reproduces events and economy", MiniTurnChecks.SeededReplayMatches),
        new("mini-turn resolved events explain player cash and stock", MiniTurnChecks.ResolvedEventsExplainCashAndStock),
        new("rejected supplier purchase preserves preparation state", BusinessDayChecks.RejectedPurchasePreservesPreparationState),
        new("day-one seeded replay reproduces report and events", BusinessDayChecks.DayOneReplayIsDeterministic),
        new("day-one report reconciles cash, inventory and market outcomes", BusinessDayChecks.DayOneReportReconcilesEconomy),
        new("emergency supply reduces margin without changing demand", BusinessDayChecks.EmergencySupplyCostsMarginWithoutChangingDemand),
        new("living-world actors produce a causal economic trajectory", LivingWorldChecks.CausalActorTrajectoryChangesEconomy),
        new("closed BOXBOX is excluded while the living-world rival remains", LivingWorldChecks.ClosedStoreCannotBeChosenByNpcPolicy),
        new("rejected living-world commands preserve the exact snapshot", LivingWorldChecks.RejectedWorldCommandIsAtomic),
        new("living-world incremental and catch-up advances match", LivingWorldChecks.IncrementalAndCatchUpAdvanceMatch),
        new("living-world JSON save resumes without duplicate effects", LivingWorldChecks.JsonSaveResumesWithoutDuplicatingEffects),
        new("player store intervention changes autonomous consequences", LivingWorldChecks.PlayerStoreInterventionChangesNpcConsequences),
        new("living NPC market reaches BOXBOX, rival and no-purchase", LivingWorldChecks.SharedMarketCanResolveEveryTerminalAlternative),
        new("rival sale persists and conserves buyer money", LivingWorldChecks.RivalSalePersistsAndConservesBuyerMoney),
        new("128 persistent actors replay identically during catch-up", LivingWorldChecks.PersistentPopulationScaleReplaysDeterministically)
    ];
}
