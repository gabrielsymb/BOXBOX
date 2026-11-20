using Boxbox.Domain.Market;

namespace Boxbox.Application.LivingWorld;

public enum LivingWorldSchedulePhase
{
    ExternalOccurrence,
    Obligations,
    Needs,
    DecisionBarrier
}

public enum LivingWorldScheduledKind
{
    SalaryDue,
    EmployerClosed,
    FoodNeedIncreased,
    ActorDecision
}

public sealed record LivingWorldScheduledEvent(
    string StableId,
    long Minute,
    LivingWorldSchedulePhase Phase,
    LivingWorldScheduledKind Kind,
    string ActorId,
    int Value = 0);

public sealed record LivingWorldEvent(
    string StableId,
    long Minute,
    LivingWorldScheduledKind SourceKind,
    string ActorId,
    string ReasonKey,
    string? ActionId,
    long CashDeltaCents,
    int NeedDelta,
    int StoreStockDelta,
    bool KnownToActor,
    MarketPurchaseOutcomeKind? MarketOutcome = null,
    string? SelectedShopId = null);

public sealed record LivingWorldAdvanceResult(
    long CurrentMinute,
    Domain.LivingWorld.LivingWorldState State,
    IReadOnlyList<LivingWorldEvent> Events);
