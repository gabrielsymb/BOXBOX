using Boxbox.Application.Customers;
using Boxbox.Domain.Market;

namespace Boxbox.Application.Market;

/// <summary>
/// Forms the canonical set of valid competitor offers before any weighted
/// policy runs. It reads current state only for passability and emits customer
/// observations only; quantities, capacity and prices are not copied out.
/// </summary>
public sealed class AdmissibleMarketOfferGenerator
{
    public MarketOfferGenerationResult Generate(
        MarketPurchaseOpportunity opportunity,
        IReadOnlyCollection<CompetitorProfile> profiles,
        IReadOnlyCollection<CompetitorMarketState> currentStates,
        IReadOnlyCollection<CompetitorOfferObservation> permittedObservations)
    {
        ArgumentNullException.ThrowIfNull(opportunity);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(currentStates);
        ArgumentNullException.ThrowIfNull(permittedObservations);

        var profilesById = BuildProfilesById(profiles);
        var statesById = BuildStatesById(currentStates, profilesById);
        var observationsByBinding = BuildObservationsByBinding(
            opportunity,
            permittedObservations,
            profilesById,
            statesById);

        var offers = new List<AdmissibleMarketOffer>();
        var passability = new List<MarketOfferPassability>(profilesById.Count);
        foreach (var profile in profilesById.Values.OrderBy(
                     static profile => profile.CompetitorId,
                     StringComparer.Ordinal))
        {
            var reasons = new List<MarketOfferPassabilityReason>();
            if (!profile.ServesCategory(opportunity.CategoryId))
            {
                reasons.Add(MarketOfferPassabilityReason.CategoryNotServed);
            }

            if (!statesById.TryGetValue(profile.CompetitorId, out var state))
            {
                reasons.Add(MarketOfferPassabilityReason.MissingCurrentState);
                passability.Add(new MarketOfferPassability(
                    profile.CompetitorId,
                    null,
                    reasons));
                continue;
            }

            if (!state.IsOpen)
            {
                reasons.Add(MarketOfferPassabilityReason.ShopClosed);
            }

            if (state.AvailableServiceCapacity <= 0)
            {
                reasons.Add(MarketOfferPassabilityReason.NoServiceCapacity);
            }

            var matchingOffers = state.Offers
                .Where(offer => offer.Targets(opportunity))
                .OrderByDescending(offer => offer.ProductId is not null)
                .ThenBy(offer => offer.OfferId, StringComparer.Ordinal)
                .ToArray();
            if (matchingOffers.Length == 0)
            {
                reasons.Add(MarketOfferPassabilityReason.NoMatchingOffer);
                passability.Add(new MarketOfferPassability(
                    profile.CompetitorId,
                    null,
                    reasons));
                continue;
            }

            // Prefer the exact product, but fall back to a valid category-wide
            // offer when the exact offer is inactive, unavailable or unknown to
            // this customer. Preference weights never make an invalid offer pass.
            var matchingOffer = matchingOffers.FirstOrDefault(offer =>
                    offer.IsActive &&
                    offer.AvailableQuantity >= opportunity.Quantity &&
                    observationsByBinding.ContainsKey((profile.CompetitorId, offer.OfferId)))
                ?? matchingOffers[0];

            if (!matchingOffer.IsActive)
            {
                reasons.Add(MarketOfferPassabilityReason.OfferInactive);
            }

            if (matchingOffer.AvailableQuantity < opportunity.Quantity)
            {
                reasons.Add(MarketOfferPassabilityReason.InsufficientAvailability);
            }

            observationsByBinding.TryGetValue(
                (profile.CompetitorId, matchingOffer.OfferId),
                out var observation);
            if (observation is null)
            {
                reasons.Add(MarketOfferPassabilityReason.MissingPermittedObservation);
            }

            var evaluation = new MarketOfferPassability(
                profile.CompetitorId,
                matchingOffer.OfferId,
                reasons);
            passability.Add(evaluation);
            if (!evaluation.IsAdmissible)
            {
                continue;
            }

            offers.Add(new AdmissibleMarketOffer(
                observation!.ObservationId,
                profile.CompetitorId,
                matchingOffer.OfferId,
                observation.BaseUtility,
                observation.PerceivedFeatures));
        }

        offers.Sort(static (left, right) =>
        {
            var shopComparison = StringComparer.Ordinal.Compare(left.ShopId, right.ShopId);
            return shopComparison != 0
                ? shopComparison
                : StringComparer.Ordinal.Compare(left.OptionId, right.OptionId);
        });

        return new MarketOfferGenerationResult(offers, passability);
    }

    private static Dictionary<string, CompetitorProfile> BuildProfilesById(
        IReadOnlyCollection<CompetitorProfile> profiles)
    {
        var result = new Dictionary<string, CompetitorProfile>(StringComparer.Ordinal);
        foreach (var profile in profiles)
        {
            ArgumentNullException.ThrowIfNull(profile);
            if (!result.TryAdd(profile.CompetitorId, profile))
            {
                throw new ArgumentException(
                    $"Competitor profile id '{profile.CompetitorId}' is duplicated.",
                    nameof(profiles));
            }
        }

        return result;
    }

    private static Dictionary<string, CompetitorMarketState> BuildStatesById(
        IReadOnlyCollection<CompetitorMarketState> currentStates,
        IReadOnlyDictionary<string, CompetitorProfile> profilesById)
    {
        var result = new Dictionary<string, CompetitorMarketState>(StringComparer.Ordinal);
        foreach (var state in currentStates)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (!profilesById.ContainsKey(state.CompetitorId))
            {
                throw new ArgumentException(
                    $"Current state references unknown competitor '{state.CompetitorId}'.",
                    nameof(currentStates));
            }

            if (!result.TryAdd(state.CompetitorId, state))
            {
                throw new ArgumentException(
                    $"Current state id '{state.CompetitorId}' is duplicated.",
                    nameof(currentStates));
            }
        }

        return result;
    }

    private static Dictionary<(string CompetitorId, string OfferId), CompetitorOfferObservation>
        BuildObservationsByBinding(
            MarketPurchaseOpportunity opportunity,
            IReadOnlyCollection<CompetitorOfferObservation> observations,
            IReadOnlyDictionary<string, CompetitorProfile> profilesById,
            IReadOnlyDictionary<string, CompetitorMarketState> statesById)
    {
        var observationIds = new HashSet<string>(StringComparer.Ordinal);
        var result = new Dictionary<
            (string CompetitorId, string OfferId),
            CompetitorOfferObservation>();

        foreach (var observation in observations)
        {
            ArgumentNullException.ThrowIfNull(observation);
            if (!observationIds.Add(observation.ObservationId))
            {
                throw new ArgumentException(
                    $"Observation id '{observation.ObservationId}' is duplicated.",
                    nameof(observations));
            }

            if (!string.Equals(
                    observation.OpportunityId,
                    opportunity.OpportunityId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Observation '{observation.ObservationId}' belongs to a different opportunity.",
                    nameof(observations));
            }

            if (!profilesById.ContainsKey(observation.CompetitorId))
            {
                throw new ArgumentException(
                    $"Observation '{observation.ObservationId}' references an unknown competitor.",
                    nameof(observations));
            }

            if (!statesById.TryGetValue(observation.CompetitorId, out var state) ||
                !state.Offers.Any(offer => string.Equals(
                    offer.OfferId,
                    observation.OfferId,
                    StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    $"Observation '{observation.ObservationId}' references an unknown offer.",
                    nameof(observations));
            }

            if (!result.TryAdd((observation.CompetitorId, observation.OfferId), observation))
            {
                throw new ArgumentException(
                    $"More than one observation targets competitor '{observation.CompetitorId}' " +
                    $"and offer '{observation.OfferId}'.",
                    nameof(observations));
            }
        }

        return result;
    }
}
