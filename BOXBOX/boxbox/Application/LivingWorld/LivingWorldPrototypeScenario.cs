using Boxbox.Domain.LivingWorld;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;

namespace Boxbox.Application.LivingWorld;

public static class LivingWorldPrototypeScenario
{
    public const long FinalMinute = 4_560;

    public static LivingWorldSimulation Create(ulong seed, bool playerStoreOpen = true)
    {
        var actors = new[]
        {
            new SimulatedActorState("npc.lia", 200, 450, EmploymentStatus.Employed,
                wageCents: 600, debtCents: 0, maximumDebtCents: 1_000),
            new SimulatedActorState("npc.rui", 900, 200, EmploymentStatus.Employed,
                wageCents: 500, debtCents: 0, maximumDebtCents: 500)
        };
        var store = new StoreState(
            "boxbox",
            playerStoreOpen,
            new Dictionary<string, int>(StringComparer.Ordinal) { ["meal"] = 8 },
            new Dictionary<string, long>(StringComparer.Ordinal) { ["meal"] = 500 },
            cashBalanceCents: 5_000);
        var competitor = new CompetitorMarketState(
            "rival-market",
            isOpen: true,
            availableServiceCapacity: 20,
            [new CompetitorOfferState(
                "rival-meal",
                "essentials",
                "meal",
                unitPriceCents: 450,
                availableQuantity: 20,
                isActive: true)]);
        var state = new LivingWorldState(actors, store, employerCashCents: 10_000,
            bankCashCents: 5_000, competitors: [competitor]);

        return new LivingWorldSimulation(seed, 0, state, BuildSchedule());
    }

    private static IEnumerable<LivingWorldScheduledEvent> BuildSchedule()
    {
        yield return Event("day1/lia/salary", 60, LivingWorldSchedulePhase.Obligations,
            LivingWorldScheduledKind.SalaryDue, "npc.lia");
        yield return Event("day1/rui/salary", 70, LivingWorldSchedulePhase.Obligations,
            LivingWorldScheduledKind.SalaryDue, "npc.rui");
        foreach (var item in NeedAndDecision("day1", 120, 180)) yield return item;

        yield return Event("day2/lia/employer-closed", 1_500,
            LivingWorldSchedulePhase.ExternalOccurrence, LivingWorldScheduledKind.EmployerClosed, "npc.lia");
        foreach (var item in NeedAndDecision("day2", 1_560, 1_620)) yield return item;

        foreach (var item in NeedAndDecision("day3", 3_000, 3_060)) yield return item;
        yield return Event("day3/lia/follow-up", 3_120, LivingWorldSchedulePhase.DecisionBarrier,
            LivingWorldScheduledKind.ActorDecision, "npc.lia");

        foreach (var item in NeedAndDecision("day4", 4_440, 4_500)) yield return item;
        yield return Event("day4/lia/follow-up", 4_560, LivingWorldSchedulePhase.DecisionBarrier,
            LivingWorldScheduledKind.ActorDecision, "npc.lia");
    }

    private static IEnumerable<LivingWorldScheduledEvent> NeedAndDecision(
        string day,
        long needMinute,
        long decisionMinute)
    {
        foreach (var actor in new[] { "npc.lia", "npc.rui" })
        {
            yield return Event($"{day}/{actor}/need", needMinute,
                LivingWorldSchedulePhase.Needs, LivingWorldScheduledKind.FoodNeedIncreased,
                actor, 300);
            yield return Event($"{day}/{actor}/decision", decisionMinute,
                LivingWorldSchedulePhase.DecisionBarrier, LivingWorldScheduledKind.ActorDecision, actor);
        }
    }

    private static LivingWorldScheduledEvent Event(
        string id,
        long minute,
        LivingWorldSchedulePhase phase,
        LivingWorldScheduledKind kind,
        string actorId,
        int value = 0) => new(id, minute, phase, kind, actorId, value);
}
