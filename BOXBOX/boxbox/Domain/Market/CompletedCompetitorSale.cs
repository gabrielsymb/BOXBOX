namespace Boxbox.Domain.Market;

public sealed record CompletedCompetitorSale(
    string DecisionId,
    string CustomerId,
    string CompetitorId,
    string OfferId,
    string ProductId,
    int Quantity,
    long UnitPriceCents,
    long TotalCents);
