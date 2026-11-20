using Boxbox.Domain.Commerce;

namespace Boxbox.Domain.Market;

public sealed class MarketResolutionAttempt
{
    private MarketResolutionAttempt(
        bool completed,
        SharedMarketState resultingState,
        MarketOpportunityResolved? resolution,
        CompletedSale? playerSale,
        CompletedCompetitorSale? competitorSale,
        MarketResolutionRejectionReason? rejectionReason,
        SaleRejectionReason? playerSaleRejectionReason,
        CompetitorSaleRejectionReason? competitorSaleRejectionReason)
    {
        Completed = completed;
        ResultingState = resultingState;
        Resolution = resolution;
        PlayerSale = playerSale;
        CompetitorSale = competitorSale;
        RejectionReason = rejectionReason;
        PlayerSaleRejectionReason = playerSaleRejectionReason;
        CompetitorSaleRejectionReason = competitorSaleRejectionReason;
    }

    public bool Completed { get; }
    public SharedMarketState ResultingState { get; }
    public MarketOpportunityResolved? Resolution { get; }
    public CompletedSale? PlayerSale { get; }
    public CompletedCompetitorSale? CompetitorSale { get; }
    public MarketResolutionRejectionReason? RejectionReason { get; }
    public SaleRejectionReason? PlayerSaleRejectionReason { get; }
    public CompetitorSaleRejectionReason? CompetitorSaleRejectionReason { get; }

    internal static MarketResolutionAttempt Succeeded(
        SharedMarketState state,
        MarketOpportunityResolved resolution,
        CompletedSale? playerSale,
        CompletedCompetitorSale? competitorSale) =>
        new(true, state, resolution, playerSale, competitorSale, null, null, null);

    internal static MarketResolutionAttempt Rejected(
        SharedMarketState unchangedState,
        MarketResolutionRejectionReason reason,
        SaleRejectionReason? playerSaleReason = null,
        CompetitorSaleRejectionReason? competitorSaleReason = null) =>
        new(
            false,
            unchangedState,
            null,
            null,
            null,
            reason,
            playerSaleReason,
            competitorSaleReason);
}
