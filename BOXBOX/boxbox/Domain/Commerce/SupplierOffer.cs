namespace Boxbox.Domain.Commerce;

public enum SupplierChannel
{
    Regular,
    Emergency
}

/// <summary>
/// Immutable supplier content. Player-facing supplier and product names are
/// localization keys owned by presentation, not by this economic contract.
/// </summary>
public sealed class SupplierOffer
{
    public SupplierOffer(
        string offerId,
        string supplierId,
        string productId,
        SupplierChannel channel,
        long unitCostCents,
        int minimumQuantity,
        int maximumQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(offerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        if (!Enum.IsDefined(channel))
        {
            throw new ArgumentOutOfRangeException(nameof(channel));
        }

        if (unitCostCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitCostCents));
        }

        if (minimumQuantity <= 0 || maximumQuantity < minimumQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumQuantity));
        }

        OfferId = offerId;
        SupplierId = supplierId;
        ProductId = productId;
        Channel = channel;
        UnitCostCents = unitCostCents;
        MinimumQuantity = minimumQuantity;
        MaximumQuantity = maximumQuantity;
    }

    public string OfferId { get; }
    public string SupplierId { get; }
    public string ProductId { get; }
    public SupplierChannel Channel { get; }
    public long UnitCostCents { get; }
    public int MinimumQuantity { get; }
    public int MaximumQuantity { get; }
}
