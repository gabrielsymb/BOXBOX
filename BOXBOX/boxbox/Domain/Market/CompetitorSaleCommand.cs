namespace Boxbox.Domain.Market;

public sealed class CompetitorSaleCommand
{
    public CompetitorSaleCommand(
        string decisionId,
        string customerId,
        string competitorId,
        string offerId,
        string categoryId,
        string productId,
        int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(competitorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(offerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Competitor sale quantity must be positive.");
        }

        DecisionId = decisionId;
        CustomerId = customerId;
        CompetitorId = competitorId;
        OfferId = offerId;
        CategoryId = categoryId;
        ProductId = productId;
        Quantity = quantity;
    }

    public string DecisionId { get; }
    public string CustomerId { get; }
    public string CompetitorId { get; }
    public string OfferId { get; }
    public string CategoryId { get; }
    public string ProductId { get; }
    public int Quantity { get; }
}
