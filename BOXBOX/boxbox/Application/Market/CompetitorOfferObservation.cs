using Boxbox.AI.Contracts;

namespace Boxbox.Application.Market;

/// <summary>
/// The customer-visible observation permitted for one competitor offer. The
/// generated choice uses these perceived values rather than private market
/// state. ObservationId also becomes the stable choice option id.
/// </summary>
public sealed class CompetitorOfferObservation
{
    public CompetitorOfferObservation(
        string observationId,
        string opportunityId,
        string competitorId,
        string offerId,
        int baseUtility,
        ChoiceFeatures perceivedFeatures)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(observationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(opportunityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(competitorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(offerId);
        if (baseUtility is < -1000 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseUtility),
                "Observed base utility must be between -1000 and 1000.");
        }

        perceivedFeatures.Validate();
        ObservationId = observationId;
        OpportunityId = opportunityId;
        CompetitorId = competitorId;
        OfferId = offerId;
        BaseUtility = baseUtility;
        PerceivedFeatures = perceivedFeatures;
    }

    public string ObservationId { get; }
    public string OpportunityId { get; }
    public string CompetitorId { get; }
    public string OfferId { get; }
    public int BaseUtility { get; }
    public ChoiceFeatures PerceivedFeatures { get; }
}
