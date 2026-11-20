namespace Boxbox.Domain.Market;

public enum MarketResolutionRejectionReason
{
    UnknownOpportunity,
    OpportunityAlreadyResolved,
    DuplicateDecision,
    ProductRequired,
    PlayerSaleRejected,
    UnknownCompetitor,
    CompetitorSaleRejected
}
