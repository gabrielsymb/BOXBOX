using Boxbox.Domain.Market;

namespace Boxbox.Application.MiniTurn;

public enum MiniTurnEventKind
{
    CustomerArrived,
    CustomerResolved
}

/// <summary>
/// Presentation-safe projection of a confirmed simulation event. It carries
/// semantic identifiers and values, never translated text.
/// </summary>
public sealed record MiniTurnEvent(
    string StableId,
    long SimulationMinute,
    MiniTurnEventKind Kind,
    string CustomerId,
    string ProductId,
    MarketPurchaseOutcomeKind? Outcome,
    string? SelectedShopId,
    int Quantity,
    long? TotalCents,
    long PlayerCashBalanceCents,
    int PlayerRemainingStock);
