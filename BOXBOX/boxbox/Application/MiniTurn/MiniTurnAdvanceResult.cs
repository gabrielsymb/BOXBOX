using Boxbox.Domain.Market;

namespace Boxbox.Application.MiniTurn;

public sealed record MiniTurnAdvanceResult(
    long SimulationMinute,
    SharedMarketState MarketState,
    IReadOnlyList<MiniTurnEvent> Events);
