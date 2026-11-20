namespace Boxbox.Domain.Market;

/// <summary>
/// Confirms an intention selected by a behavior engine. Product and quantity
/// are deliberately absent: the authoritative opportunity supplies them.
/// </summary>
public sealed class ResolveMarketPurchaseCommand
{
    public ResolveMarketPurchaseCommand(
        string opportunityId,
        string decisionId,
        string customerId,
        MarketPurchaseOutcomeKind outcome,
        string? selectedShopId = null,
        string? selectedOfferId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opportunityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        switch (outcome)
        {
            case MarketPurchaseOutcomeKind.PlayerStoreSale:
                ArgumentException.ThrowIfNullOrWhiteSpace(selectedShopId);
                if (selectedOfferId is not null)
                {
                    throw new ArgumentException(
                        "A player-store sale cannot identify a competitor offer.",
                        nameof(selectedOfferId));
                }
                break;

            case MarketPurchaseOutcomeKind.CompetitorSale:
                ArgumentException.ThrowIfNullOrWhiteSpace(selectedShopId);
                ArgumentException.ThrowIfNullOrWhiteSpace(selectedOfferId);
                break;

            case MarketPurchaseOutcomeKind.ChoseNotToPurchase:
            case MarketPurchaseOutcomeKind.DemandUnserved:
                if (selectedShopId is not null || selectedOfferId is not null)
                {
                    throw new ArgumentException(
                        "A no-sale outcome cannot identify a shop or offer.");
                }
                break;
        }

        OpportunityId = opportunityId;
        DecisionId = decisionId;
        CustomerId = customerId;
        Outcome = outcome;
        SelectedShopId = selectedShopId;
        SelectedOfferId = selectedOfferId;
    }

    public string OpportunityId { get; }
    public string DecisionId { get; }
    public string CustomerId { get; }
    public MarketPurchaseOutcomeKind Outcome { get; }
    public string? SelectedShopId { get; }
    public string? SelectedOfferId { get; }
}
