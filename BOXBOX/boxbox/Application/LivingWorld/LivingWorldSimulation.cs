using Boxbox.AI.LivingWorld;
using Boxbox.AI.Randomness;
using Boxbox.Domain.LivingWorld;

namespace Boxbox.Application.LivingWorld;

public sealed class LivingWorldSimulation
{
    public const string CurrentRulesetVersion = "living-world-v2-shared-market";

    private readonly ulong _seed;
    private readonly INpcDecisionPolicy _decisionPolicy;
    private readonly LivingWorldTransactionService _transactions;
    private readonly LivingWorldMarketPurchaseService _marketPurchases;
    private readonly IReadOnlyList<LivingWorldScheduledEvent> _schedule;
    private readonly HashSet<string> _processedEventIds;
    private readonly List<LivingWorldEvent> _eventLog;
    private LivingWorldState _state;

    public LivingWorldSimulation(
        ulong seed,
        long currentMinute,
        LivingWorldState initialState,
        IEnumerable<LivingWorldScheduledEvent> schedule,
        INpcDecisionPolicy? decisionPolicy = null,
        LivingWorldTransactionService? transactions = null)
        : this(seed, currentMinute, initialState, schedule, [], [], decisionPolicy, transactions)
    {
    }

    private LivingWorldSimulation(
        ulong seed,
        long currentMinute,
        LivingWorldState initialState,
        IEnumerable<LivingWorldScheduledEvent> schedule,
        IEnumerable<string> processedEventIds,
        IEnumerable<LivingWorldEvent> eventLog,
        INpcDecisionPolicy? decisionPolicy,
        LivingWorldTransactionService? transactions)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(schedule);
        if (currentMinute < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentMinute));
        }

        _seed = seed;
        CurrentMinute = currentMinute;
        _state = initialState;
        _decisionPolicy = decisionPolicy ?? new WeightedNpcDecisionPolicy();
        _transactions = transactions ?? new LivingWorldTransactionService();
        _marketPurchases = new LivingWorldMarketPurchaseService(_transactions);
        _schedule = Array.AsReadOnly(schedule
            .OrderBy(item => item.Minute)
            .ThenBy(item => item.Phase)
            .ThenBy(item => item.StableId, StringComparer.Ordinal)
            .ToArray());
        if (_schedule.Select(item => item.StableId).Distinct(StringComparer.Ordinal).Count() != _schedule.Count)
        {
            throw new ArgumentException("Scheduled event ids must be unique.", nameof(schedule));
        }

        _processedEventIds = new HashSet<string>(processedEventIds, StringComparer.Ordinal);
        _eventLog = new List<LivingWorldEvent>(eventLog);
    }

    public long CurrentMinute { get; private set; }
    public LivingWorldState State => _state;
    public IReadOnlyList<LivingWorldEvent> EventLog => _eventLog.AsReadOnly();

    public static LivingWorldSimulation Restore(
        LivingWorldSnapshot snapshot,
        INpcDecisionPolicy? decisionPolicy = null,
        LivingWorldTransactionService? transactions = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!string.Equals(snapshot.RulesetVersion, CurrentRulesetVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("world.snapshot.ruleset_unsupported");
        }

        return new LivingWorldSimulation(snapshot.Seed, snapshot.CurrentMinute, snapshot.State,
            snapshot.Schedule, snapshot.ProcessedEventIds, snapshot.EventLog, decisionPolicy, transactions);
    }

    public LivingWorldSnapshot CreateSnapshot() => new(
        CurrentRulesetVersion,
        _seed,
        CurrentMinute,
        _state,
        _schedule,
        _processedEventIds.Order(StringComparer.Ordinal).ToArray(),
        _eventLog.ToArray());

    public LivingWorldAdvanceResult AdvanceTo(long targetMinute)
    {
        if (targetMinute < CurrentMinute)
        {
            throw new ArgumentOutOfRangeException(nameof(targetMinute), "Simulation time is monotonic.");
        }

        var emitted = new List<LivingWorldEvent>();
        foreach (var scheduled in _schedule)
        {
            if (scheduled.Minute > targetMinute || _processedEventIds.Contains(scheduled.StableId))
            {
                continue;
            }

            var worldEvent = Process(scheduled);
            _processedEventIds.Add(scheduled.StableId);
            _eventLog.Add(worldEvent);
            emitted.Add(worldEvent);
        }

        CurrentMinute = targetMinute;
        return new LivingWorldAdvanceResult(CurrentMinute, _state, Array.AsReadOnly(emitted.ToArray()));
    }

    public LivingWorldEvent ApplyPlayerStoreCommand(string commandId, bool isOpen)
    {
        var result = _transactions.Execute(_state, new SetStoreOpenCommand(commandId, isOpen));
        if (!result.Completed || result.Effect is null)
        {
            throw new InvalidOperationException($"Player store command rejected: {result.RejectionReason}.");
        }

        _state = result.ResultingState;
        var projected = ProjectEffect(commandId, CurrentMinute, LivingWorldScheduledKind.ActorDecision,
            result.Effect, commandId, knownToActor: true);
        _eventLog.Add(projected);
        return projected;
    }

    private LivingWorldEvent Process(LivingWorldScheduledEvent scheduled)
    {
        if (scheduled.Kind == LivingWorldScheduledKind.ActorDecision)
        {
            return ProcessDecision(scheduled);
        }

        LivingWorldCommand command = scheduled.Kind switch
        {
            LivingWorldScheduledKind.SalaryDue => new PaySalaryCommand(scheduled.StableId, scheduled.ActorId),
            LivingWorldScheduledKind.EmployerClosed => new LoseJobCommand(
                scheduled.StableId, scheduled.ActorId, $"fact/{scheduled.StableId}"),
            LivingWorldScheduledKind.FoodNeedIncreased => new IncreaseFoodNeedCommand(
                scheduled.StableId, scheduled.ActorId, scheduled.Value),
            _ => throw new InvalidOperationException("Unsupported scheduled world event.")
        };
        return ExecuteScheduledCommand(scheduled, command, scheduled.StableId);
    }

    private LivingWorldEvent ProcessDecision(LivingWorldScheduledEvent scheduled)
    {
        var candidates = GenerateValidActions(scheduled).ToArray();
        var request = new NpcDecisionRequest(scheduled.StableId, scheduled.ActorId,
            Array.AsReadOnly(candidates));
        var proposal = _decisionPolicy.Propose(request,
            new SeededChoiceRandomSource(DeriveEventSeed(_seed, scheduled.StableId, "decision")));

        if (!candidates.Any(item => item == proposal.SelectedAction))
        {
            throw new InvalidOperationException("world.decision.proposed_action_not_in_valid_set");
        }

        var action = proposal.SelectedAction;
        if (action.Kind == NpcActionKind.Wait)
        {
            return new LivingWorldEvent(scheduled.StableId, scheduled.Minute, scheduled.Kind,
                scheduled.ActorId, action.ReasonKey, action.ActionId, 0, 0, 0, true);
        }

        if (action.Kind == NpcActionKind.PurchaseMeal)
        {
            var marketResult = _marketPurchases.Execute(
                _state,
                scheduled.StableId,
                scheduled.ActorId,
                DeriveEventSeed(_seed, scheduled.StableId, "market-choice"));
            if (!marketResult.Completed || marketResult.Effect is null)
            {
                return new LivingWorldEvent(
                    scheduled.StableId,
                    scheduled.Minute,
                    scheduled.Kind,
                    scheduled.ActorId,
                    $"world.command.rejected.{marketResult.RejectionReason}",
                    action.ActionId,
                    0,
                    0,
                    0,
                    true);
            }

            _state = marketResult.ResultingState;
            return ProjectEffect(
                scheduled.StableId,
                scheduled.Minute,
                scheduled.Kind,
                marketResult.Effect,
                action.ActionId,
                knownToActor: true);
        }

        LivingWorldCommand command = action.Kind switch
        {
            NpcActionKind.BorrowForEssentials => new BorrowForEssentialsCommand(
                scheduled.StableId, scheduled.ActorId, action.AmountCents),
            NpcActionKind.SeekEmployment => new SeekEmploymentCommand(
                scheduled.StableId,
                scheduled.ActorId,
                new SeededChoiceRandomSource(DeriveEventSeed(_seed, scheduled.StableId, "job-outcome"))
                    .NextUnitDouble() < 0.55),
            _ => throw new InvalidOperationException("Unsupported NPC action.")
        };
        return ExecuteScheduledCommand(scheduled, command, action.ActionId);
    }

    private IEnumerable<NpcActionCandidate> GenerateValidActions(LivingWorldScheduledEvent scheduled)
    {
        var actor = _state.Actors[scheduled.ActorId];
        if (actor.FoodNeed >= 500)
        {
            yield return new NpcActionCandidate("action.shop_for_meal", NpcActionKind.PurchaseMeal,
                1200 + actor.FoodNeed, "world.decision.need_food");
        }

        var minimumMealPriceCents = MinimumAvailableMealPrice();
        if (actor.FoodNeed >= 700 && minimumMealPriceCents is not null &&
            actor.CashCents < minimumMealPriceCents &&
            actor.DebtCents + minimumMealPriceCents <= actor.MaximumDebtCents &&
            _state.BankCashCents >= minimumMealPriceCents)
        {
            yield return new NpcActionCandidate("action.borrow_for_essentials",
                NpcActionKind.BorrowForEssentials, 1000 + actor.FoodNeed,
                "world.decision.need_credit", minimumMealPriceCents.Value);
        }

        if (actor.Employment == EmploymentStatus.Unemployed)
        {
            yield return new NpcActionCandidate("action.seek_employment", NpcActionKind.SeekEmployment,
                900 + actor.FoodNeed / 2, "world.decision.unemployed");
        }

        yield return new NpcActionCandidate("action.wait", NpcActionKind.Wait, 100,
            "world.decision.wait");
    }

    private LivingWorldEvent ExecuteScheduledCommand(
        LivingWorldScheduledEvent scheduled,
        LivingWorldCommand command,
        string actionId)
    {
        var result = _transactions.Execute(_state, command);
        if (!result.Completed || result.Effect is null)
        {
            return new LivingWorldEvent(scheduled.StableId, scheduled.Minute, scheduled.Kind,
                scheduled.ActorId, $"world.command.rejected.{result.RejectionReason}", actionId,
                0, 0, 0, true);
        }

        _state = result.ResultingState;
        return ProjectEffect(scheduled.StableId, scheduled.Minute, scheduled.Kind,
            result.Effect, actionId, knownToActor: true);
    }

    private static LivingWorldEvent ProjectEffect(
        string stableId,
        long minute,
        LivingWorldScheduledKind sourceKind,
        LivingWorldEffect effect,
        string actionId,
        bool knownToActor) => new(stableId, minute, sourceKind, effect.ActorId,
            effect.ReasonKey, actionId, effect.CashDeltaCents, effect.NeedDelta,
            effect.StoreStockDelta, knownToActor, effect.MarketOutcome, effect.SelectedShopId);

    private long? MinimumAvailableMealPrice()
    {
        var prices = new List<long>();
        if (_state.Store.IsOpen && _state.Store.StockByProduct.GetValueOrDefault("meal") > 0 &&
            _state.Store.TryGetUnitPriceCents("meal", out var playerPrice))
        {
            prices.Add(playerPrice);
        }

        prices.AddRange(_state.Competitors.Values
            .Where(item => item.IsOpen && item.AvailableServiceCapacity > 0)
            .SelectMany(item => item.Offers)
            .Where(item => item.IsActive && item.AvailableQuantity > 0 &&
                string.Equals(item.CategoryId, "essentials", StringComparison.Ordinal) &&
                (item.ProductId is null || string.Equals(item.ProductId, "meal", StringComparison.Ordinal)))
            .Select(item => item.UnitPriceCents));
        return prices.Count == 0 ? null : prices.Min();
    }

    private static ulong DeriveEventSeed(ulong baseSeed, string stableId, string stream)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;
        foreach (var character in $"{stableId}/{stream}")
        {
            hash ^= character;
            hash *= prime;
        }

        return baseSeed ^ hash;
    }
}
