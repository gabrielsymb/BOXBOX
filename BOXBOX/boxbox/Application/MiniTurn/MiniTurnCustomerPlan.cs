using Boxbox.AI.Contracts;
using Boxbox.Application.Market;

namespace Boxbox.Application.MiniTurn;

/// <summary>
/// Immutable content for one customer in a short simulated turn. Times use
/// whole simulation minutes; presentation speed never changes these values.
/// </summary>
public sealed class MiniTurnCustomerPlan
{
    private readonly IReadOnlyList<CompetitorOfferObservation> _competitorObservations;

    public MiniTurnCustomerPlan(
        string customerId,
        string opportunityId,
        string decisionId,
        long arrivalMinute,
        long decisionMinute,
        CustomerPreferenceProfile preferenceProfile,
        ChoiceFeatures playerStorePerceivedFeatures,
        IEnumerable<CompetitorOfferObservation> competitorObservations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(opportunityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentNullException.ThrowIfNull(preferenceProfile);
        ArgumentNullException.ThrowIfNull(competitorObservations);
        if (arrivalMinute < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrivalMinute));
        }

        if (decisionMinute < arrivalMinute)
        {
            throw new ArgumentOutOfRangeException(
                nameof(decisionMinute),
                "A customer cannot decide before arriving.");
        }

        playerStorePerceivedFeatures.Validate();
        CustomerId = customerId;
        OpportunityId = opportunityId;
        DecisionId = decisionId;
        ArrivalMinute = arrivalMinute;
        DecisionMinute = decisionMinute;
        PreferenceProfile = preferenceProfile;
        PlayerStorePerceivedFeatures = playerStorePerceivedFeatures;
        _competitorObservations = Array.AsReadOnly(competitorObservations.ToArray());
    }

    public string CustomerId { get; }
    public string OpportunityId { get; }
    public string DecisionId { get; }
    public long ArrivalMinute { get; }
    public long DecisionMinute { get; }
    public CustomerPreferenceProfile PreferenceProfile { get; }
    public ChoiceFeatures PlayerStorePerceivedFeatures { get; }
    public IReadOnlyList<CompetitorOfferObservation> CompetitorObservations => _competitorObservations;
}
