using Boxbox.Application.Market;
using Boxbox.Domain.Market;
using static Boxbox.Verification.MarketFixtures;
using static Boxbox.Verification.VerificationAssert;

namespace Boxbox.Verification;

internal static class MarketOfferPassabilityChecks
{
    internal static void PassabilityGuardsFilterInvalidOffers()
    {
        var opportunity = StandardOpportunity("passability-opportunity");

        RequireFiltered(
            opportunity,
            StandardCompetitorProfile("missing-state"),
            state: null,
            observation: null,
            MarketOfferPassabilityReason.MissingCurrentState);

        var wrongCategoryProfile = StandardCompetitorProfile("wrong-category", "cosmetics");
        var wrongCategoryState = StandardCompetitorState("wrong-category");
        RequireFiltered(
            opportunity,
            wrongCategoryProfile,
            wrongCategoryState,
            StandardObservation(opportunity, "wrong-category", "rival-rice"),
            MarketOfferPassabilityReason.CategoryNotServed);

        var closedState = StandardCompetitorState("closed", isOpen: false);
        RequireFiltered(
            opportunity,
            StandardCompetitorProfile("closed"),
            closedState,
            StandardObservation(opportunity, "closed", "rival-rice"),
            MarketOfferPassabilityReason.ShopClosed);

        var noCapacityState = StandardCompetitorState("no-capacity", availableServiceCapacity: 0);
        RequireFiltered(
            opportunity,
            StandardCompetitorProfile("no-capacity"),
            noCapacityState,
            StandardObservation(opportunity, "no-capacity", "rival-rice"),
            MarketOfferPassabilityReason.NoServiceCapacity);

        var unrelatedOffer = StandardCompetitorOffer("rival-beans", productId: "beans");
        var noMatchState = StandardCompetitorState("no-match", offers: [unrelatedOffer]);
        RequireFiltered(
            opportunity,
            StandardCompetitorProfile("no-match"),
            noMatchState,
            StandardObservation(opportunity, "no-match", unrelatedOffer.OfferId),
            MarketOfferPassabilityReason.NoMatchingOffer);

        var inactiveOffer = StandardCompetitorOffer("rival-rice", isActive: false);
        var inactiveState = StandardCompetitorState("inactive", offers: [inactiveOffer]);
        RequireFiltered(
            opportunity,
            StandardCompetitorProfile("inactive"),
            inactiveState,
            StandardObservation(opportunity, "inactive", inactiveOffer.OfferId),
            MarketOfferPassabilityReason.OfferInactive);

        var unavailableOffer = StandardCompetitorOffer("rival-rice", availableQuantity: 0);
        var unavailableState = StandardCompetitorState("unavailable", offers: [unavailableOffer]);
        RequireFiltered(
            opportunity,
            StandardCompetitorProfile("unavailable"),
            unavailableState,
            StandardObservation(opportunity, "unavailable", unavailableOffer.OfferId),
            MarketOfferPassabilityReason.InsufficientAvailability);

        RequireFiltered(
            opportunity,
            StandardCompetitorProfile("unobserved"),
            StandardCompetitorState("unobserved"),
            observation: null,
            MarketOfferPassabilityReason.MissingPermittedObservation);
    }

    internal static void CategoryOfferFallsBackFromInvalidExactOffer()
    {
        var opportunity = StandardOpportunity("category-fallback-opportunity");
        var exactOffer = StandardCompetitorOffer(
            "exact-rice",
            productId: "rice",
            availableQuantity: 0);
        var categoryOffer = StandardCompetitorOffer("grocery-category", productId: null);
        var result = new AdmissibleMarketOfferGenerator().Generate(
            opportunity,
            [StandardCompetitorProfile("rival-market")],
            [StandardCompetitorState("rival-market", offers: [categoryOffer, exactOffer])],
            [
                StandardObservation(opportunity, "rival-market", exactOffer.OfferId, "option-exact"),
                StandardObservation(opportunity, "rival-market", categoryOffer.OfferId, "option-category")
            ]);

        Require(result.AdmissibleOffers.Count == 1, "A valid category fallback was not offered.");
        Require(result.AdmissibleOffers[0].OptionId == "option-category",
            "An invalid exact offer hid the valid category fallback.");
        Require(result.Passability.Single().OfferId == categoryOffer.OfferId,
            "Passability diagnostics did not identify the category fallback used.");
    }

    internal static void ExactOfferTakesPrecedence()
    {
        var opportunity = StandardOpportunity("exact-precedence-opportunity");
        var exactOffer = StandardCompetitorOffer("exact-rice", productId: "rice");
        var categoryOffer = StandardCompetitorOffer("grocery-category", productId: null);
        var result = new AdmissibleMarketOfferGenerator().Generate(
            opportunity,
            [StandardCompetitorProfile("rival-market")],
            [StandardCompetitorState("rival-market", offers: [categoryOffer, exactOffer])],
            [
                StandardObservation(opportunity, "rival-market", categoryOffer.OfferId, "option-category"),
                StandardObservation(opportunity, "rival-market", exactOffer.OfferId, "option-exact")
            ]);

        Require(result.AdmissibleOffers.Single().OptionId == "option-exact",
            "A category-wide offer replaced a valid exact product offer.");
    }

    internal static void MarketOfferOrderIsCanonical()
    {
        var opportunity = StandardOpportunity("canonical-order-opportunity");
        var profileA = StandardCompetitorProfile("a-market");
        var profileB = StandardCompetitorProfile("b-market");
        var stateA = StandardCompetitorState("a-market");
        var stateB = StandardCompetitorState("b-market");
        var observationA = StandardObservation(opportunity, "a-market", "rival-rice", "z-option");
        var observationB = StandardObservation(opportunity, "b-market", "rival-rice", "a-option");
        var generator = new AdmissibleMarketOfferGenerator();

        var forward = generator.Generate(
            opportunity,
            [profileA, profileB],
            [stateA, stateB],
            [observationA, observationB]);
        var reversed = generator.Generate(
            opportunity,
            [profileB, profileA],
            [stateB, stateA],
            [observationB, observationA]);

        var forwardIds = forward.AdmissibleOffers.Select(offer => $"{offer.ShopId}:{offer.OptionId}").ToArray();
        var reversedIds = reversed.AdmissibleOffers.Select(offer => $"{offer.ShopId}:{offer.OptionId}").ToArray();
        Require(forwardIds.SequenceEqual(reversedIds), "Input order changed the generated market order.");
        Require(forwardIds.SequenceEqual(["a-market:z-option", "b-market:a-option"]),
            "Generated offers are not ordered canonically by competitor id.");
    }

    private static void RequireFiltered(
        MarketPurchaseOpportunity opportunity,
        CompetitorProfile profile,
        CompetitorMarketState? state,
        CompetitorOfferObservation? observation,
        MarketOfferPassabilityReason expectedReason)
    {
        IReadOnlyCollection<CompetitorMarketState> states = state is null
            ? Array.Empty<CompetitorMarketState>()
            : [state];
        IReadOnlyCollection<CompetitorOfferObservation> observations = observation is null
            ? Array.Empty<CompetitorOfferObservation>()
            : [observation];
        var result = new AdmissibleMarketOfferGenerator().Generate(
            opportunity,
            [profile],
            states,
            observations);

        Require(result.AdmissibleOffers.Count == 0,
            $"An offer blocked by {expectedReason} entered the admissible set.");
        var diagnostic = result.Passability.Single();
        Require(diagnostic.Reasons.Contains(expectedReason),
            $"Passability did not record expected reason {expectedReason}.");
    }
}
