namespace Boxbox.Domain.Market;

public sealed class MarketOpportunityRecord
{
    internal MarketOpportunityRecord(
        MarketPurchaseOpportunity opportunity,
        MarketOpportunityResolved? resolution = null)
    {
        ArgumentNullException.ThrowIfNull(opportunity);
        if (resolution is not null && !string.Equals(
                resolution.OpportunityId,
                opportunity.OpportunityId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A resolution must belong to its market opportunity.",
                nameof(resolution));
        }

        Opportunity = opportunity;
        Resolution = resolution;
    }

    public MarketPurchaseOpportunity Opportunity { get; }
    public MarketOpportunityResolved? Resolution { get; }
    public MarketOpportunityStatus Status => Resolution is null
        ? MarketOpportunityStatus.Pending
        : MarketOpportunityStatus.Resolved;

    internal MarketOpportunityRecord Resolve(MarketOpportunityResolved resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        if (Resolution is not null)
        {
            throw new InvalidOperationException(
                $"Market opportunity '{Opportunity.OpportunityId}' is already resolved.");
        }

        return new MarketOpportunityRecord(Opportunity, resolution);
    }
}
