namespace Boxbox.Domain.Commerce;

public sealed record CompletedSale(
    string DecisionId,
    string CustomerId,
    string ShopId,
    string ProductId,
    int Quantity,
    long UnitPriceCents,
    long TotalCents);
