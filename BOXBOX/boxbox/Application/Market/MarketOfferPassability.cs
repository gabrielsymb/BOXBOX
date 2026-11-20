using System.Collections.ObjectModel;

namespace Boxbox.Application.Market;

/// <summary>
/// Internal diagnostic for one competitor in one opportunity. It may describe
/// unobserved real state and must not be rendered as player knowledge.
/// Empty Reasons means that the selected offer is admissible.
/// </summary>
internal sealed class MarketOfferPassability
{
    private readonly IReadOnlyList<MarketOfferPassabilityReason> _reasons;

    internal MarketOfferPassability(
        string competitorId,
        string? offerId,
        IEnumerable<MarketOfferPassabilityReason> reasons)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competitorId);
        ArgumentNullException.ThrowIfNull(reasons);

        var copiedReasons = reasons
            .Distinct()
            .OrderBy(static reason => reason)
            .ToList();

        CompetitorId = competitorId;
        OfferId = offerId;
        _reasons = new ReadOnlyCollection<MarketOfferPassabilityReason>(copiedReasons);
    }

    public string CompetitorId { get; }
    public string? OfferId { get; }
    public IReadOnlyList<MarketOfferPassabilityReason> Reasons => _reasons;
    public bool IsAdmissible => _reasons.Count == 0;
}
