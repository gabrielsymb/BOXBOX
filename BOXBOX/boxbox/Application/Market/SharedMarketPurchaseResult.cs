using Boxbox.Application.Customers;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;

namespace Boxbox.Application.Market;

public enum SharedMarketPurchaseStatus
{
    Resolved,
    Rejected
}

/// <summary>
/// Result of one authoritative shared-market attempt. Successful results carry
/// one complete replacement snapshot; rejected results retain the exact input
/// snapshot and may occur before customer choice.
/// </summary>
public sealed class SharedMarketPurchaseResult
{
    internal SharedMarketPurchaseResult(
        SharedMarketPurchaseStatus status,
        MarketResolutionAttempt resolutionAttempt,
        MarketOfferGenerationResult? offerGeneration,
        CustomerPurchaseFlowResult? purchaseFlow)
    {
        ArgumentNullException.ThrowIfNull(resolutionAttempt);

        Status = status;
        ResolutionAttempt = resolutionAttempt;
        OfferGeneration = offerGeneration;
        PurchaseFlow = purchaseFlow;
    }

    public SharedMarketPurchaseStatus Status { get; }
    public MarketResolutionAttempt ResolutionAttempt { get; }
    public MarketOfferGenerationResult? OfferGeneration { get; }
    public CustomerPurchaseFlowResult? PurchaseFlow { get; }
    public SharedMarketState ResultingMarketState => ResolutionAttempt.ResultingState;
    public StoreState ResultingPlayerStoreState => ResultingMarketState.PlayerStore;
    public MarketOpportunityResolved? Resolution => ResolutionAttempt.Resolution;
    public MarketResolutionRejectionReason? RejectionReason => ResolutionAttempt.RejectionReason;
    public CompletedCompetitorSale? CompetitorSale => ResolutionAttempt.CompetitorSale;
}
