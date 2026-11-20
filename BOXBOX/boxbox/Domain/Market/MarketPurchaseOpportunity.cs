namespace Boxbox.Domain.Market;

/// <summary>
/// One demand opportunity to be offered to admissible shops. ProductId is
/// optional so the same contract supports a concrete product or a category.
/// </summary>
public sealed class MarketPurchaseOpportunity
{
    public MarketPurchaseOpportunity(
        string opportunityId,
        string categoryId,
        string? productId,
        int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opportunityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
        if (productId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "A market opportunity quantity must be positive.");
        }

        OpportunityId = opportunityId;
        CategoryId = categoryId;
        ProductId = productId;
        Quantity = quantity;
    }

    public string OpportunityId { get; }
    public string CategoryId { get; }
    public string? ProductId { get; }
    public int Quantity { get; }
    public bool TargetsSpecificProduct => ProductId is not null;
}
