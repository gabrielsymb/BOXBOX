using Boxbox.Application.MiniTurn;
using Boxbox.Domain.Commerce;

namespace Boxbox.Application.BusinessDay;

/// <summary>The running Day 1 simulation and its confirmed events.</summary>
public sealed class GroceryTradingDay
{
    private readonly ulong _seed;
    private readonly MiniTurnSimulation _simulation;
    private readonly IReadOnlyCollection<CompletedInventoryPurchase> _purchases;
    private readonly Queue<long> _decisionMinutes;
    private readonly List<MiniTurnEvent> _events = [];
    private BusinessDayRunResult? _result;

    internal GroceryTradingDay(
        ulong seed,
        MiniTurnSimulation simulation,
        IReadOnlyCollection<CompletedInventoryPurchase> purchases,
        IEnumerable<long> decisionMinutes)
    {
        _seed = seed;
        _simulation = simulation;
        _purchases = purchases;
        _decisionMinutes = new Queue<long>(decisionMinutes.Distinct().Order());
    }

    public long Minute => _result is null ? _simulation.CurrentMinute : GroceryDayOneScenario.ClosingMinute;
    public long CashCents => _result?.Report.ClosingCashCents ?? _simulation.MarketState.PlayerStore.CashBalanceCents;
    public MiniTurnEvent? LatestResolution { get; private set; }
    public bool HasPendingCustomers => _result is null && _decisionMinutes.Count > 0;

    public void AdvanceToNextCustomer()
    {
        if (_result is null && _decisionMinutes.TryDequeue(out var minute))
        {
            var advance = _simulation.AdvanceTo(minute);
            _events.AddRange(advance.Events);
            LatestResolution = advance.Events.Last(item => item.Kind == MiniTurnEventKind.CustomerResolved);
        }
    }

    public BusinessDayRunResult Close()
    {
        if (_result is not null)
        {
            return _result;
        }

        var advance = _simulation.AdvanceTo(GroceryDayOneScenario.ClosingMinute);
        _events.AddRange(advance.Events);
        var closed = advance.MarketState.ClosePlayerStore();
        var report = BusinessDayReport.Create(_seed, GroceryDayOneScenario.OpeningCashCents, _purchases, _events, closed);
        _result = new BusinessDayRunResult(report, Array.AsReadOnly(_events.ToArray()), closed);
        return _result;
    }
}
