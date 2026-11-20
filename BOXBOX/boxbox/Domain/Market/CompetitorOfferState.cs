namespace Boxbox.Domain.Market;

/// <summary>
/// Current summarized offer of a competitor. A null ProductId means that the
/// offer represents its category as a whole. Cost and cash are deliberately
/// absent; they are private and irrelevant to customer passability.
/// </summary>
public sealed class CompetitorOfferState
{
    public CompetitorOfferState(
        string offerId,
        string categoryId,
        string? productId,
        long unitPriceCents,
        int availableQuantity,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(offerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
        if (productId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        }

        if (unitPriceCents <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPriceCents),
                "A competitor offer price must be positive.");
        }

        if (availableQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableQuantity),
                "Available quantity cannot be negative.");
        }

        OfferId = offerId;
        CategoryId = categoryId;
        ProductId = productId;
        UnitPriceCents = unitPriceCents;
        AvailableQuantity = availableQuantity;
        IsActive = isActive;
    }

    public string OfferId { get; }
    public string CategoryId { get; }
    public string? ProductId { get; }
    public long UnitPriceCents { get; }
    public int AvailableQuantity { get; }
    public bool IsActive { get; }

    public bool Targets(MarketPurchaseOpportunity opportunity)
    {
        ArgumentNullException.ThrowIfNull(opportunity);

        return string.Equals(CategoryId, opportunity.CategoryId, StringComparison.Ordinal) &&
               (ProductId is null ||
                string.Equals(ProductId, opportunity.ProductId, StringComparison.Ordinal));
    }

    internal CompetitorOfferState WithAvailableQuantity(int availableQuantity) =>
        new(
            OfferId,
            CategoryId,
            ProductId,
            UnitPriceCents,
            availableQuantity,
            IsActive);
}
