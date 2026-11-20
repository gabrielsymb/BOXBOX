namespace Boxbox.Domain.Market;

/// <summary>
/// Structured causal event for the single terminal outcome of one purchase
/// opportunity. It carries stable ids and values, never localized text.
/// </summary>
public sealed class MarketOpportunityResolved
{
    internal MarketOpportunityResolved(
        string opportunityId,
        string decisionId,
        string customerId,
        MarketPurchaseOutcomeKind outcome,
        string? shopId,
        string? offerId,
        string productId,
        int quantity,
        long? unitPriceCents,
        long? totalCents)
    {
        OpportunityId = opportunityId;
        DecisionId = decisionId;
        CustomerId = customerId;
        Outcome = outcome;
        ShopId = shopId;
        OfferId = offerId;
        ProductId = productId;
        Quantity = quantity;
        UnitPriceCents = unitPriceCents;
        TotalCents = totalCents;
    }

    public string OpportunityId { get; }
    public string DecisionId { get; }
    public string CustomerId { get; }
    public MarketPurchaseOutcomeKind Outcome { get; }
    public string? ShopId { get; }
    public string? OfferId { get; }
    public string ProductId { get; }
    public int Quantity { get; }
    public long? UnitPriceCents { get; }
    public long? TotalCents { get; }
}
