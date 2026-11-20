namespace Boxbox.Domain.Commerce;

public sealed class SaleCommand
{
    public SaleCommand(string decisionId, string customerId, string shopId, string productId, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Sale quantity must be positive.");
        }

        DecisionId = decisionId;
        CustomerId = customerId;
        ShopId = shopId;
        ProductId = productId;
        Quantity = quantity;
    }

    public string DecisionId { get; }
    public string CustomerId { get; }
    public string ShopId { get; }
    public string ProductId { get; }
    public int Quantity { get; }
}
