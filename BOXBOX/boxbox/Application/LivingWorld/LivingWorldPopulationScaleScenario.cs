using Boxbox.Domain.Commerce;
using Boxbox.Domain.LivingWorld;
using Boxbox.Domain.Market;

namespace Boxbox.Application.LivingWorld;

/// <summary>
/// Headless engineering fixture for persistent-population scale. This is not
/// final balance, a population cap, or authored world content.
/// </summary>
public static class LivingWorldPopulationScaleScenario
{
    public const int DefaultActorCount = 128;
    public const long FinalMinute = 1_680;

    public static LivingWorldSimulation Create(
        ulong seed,
        int actorCount = DefaultActorCount)
    {
        if (actorCount < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(actorCount));
        }

        var actors = Enumerable.Range(0, actorCount)
            .Select(index => new SimulatedActorState(
                ActorId(index),
                cashCents: 700 + index % 5 * 100,
                foodNeed: 250 + index % 4 * 75,
                EmploymentStatus.Employed,
                wageCents: 450 + index % 3 * 50,
                debtCents: 0,
                maximumDebtCents: 1_000))
            .ToArray();
        var availableUnits = checked(actorCount * 4);
        var store = new StoreState(
            "boxbox",
            isOpen: true,
            new Dictionary<string, int>(StringComparer.Ordinal) { ["meal"] = availableUnits },
            new Dictionary<string, long>(StringComparer.Ordinal) { ["meal"] = 500 },
            cashBalanceCents: 50_000);
        var competitor = new CompetitorMarketState(
            "rival-market",
            isOpen: true,
            availableServiceCapacity: availableUnits,
            [new CompetitorOfferState(
                "rival-meal",
                "essentials",
                "meal",
                unitPriceCents: 475,
                availableQuantity: availableUnits,
                isActive: true)]);
        var state = new LivingWorldState(
            actors,
            store,
            employerCashCents: checked(actorCount * 2_000L),
            bankCashCents: checked(actorCount * 1_000L),
            competitors: [competitor]);

        return new LivingWorldSimulation(seed, 0, state, BuildSchedule(actorCount));
    }

    private static IEnumerable<LivingWorldScheduledEvent> BuildSchedule(int actorCount)
    {
        for (var index = 0; index < actorCount; index++)
        {
            var actorId = ActorId(index);
            var offset = index % 30;
            yield return Event($"day1/{actorId}/salary", 60 + offset,
                LivingWorldSchedulePhase.Obligations, LivingWorldScheduledKind.SalaryDue, actorId);
            yield return Event($"day1/{actorId}/need", 120 + offset,
                LivingWorldSchedulePhase.Needs, LivingWorldScheduledKind.FoodNeedIncreased, actorId, 300);
            yield return Event($"day1/{actorId}/decision", 180 + offset,
                LivingWorldSchedulePhase.DecisionBarrier, LivingWorldScheduledKind.ActorDecision, actorId);
            yield return Event($"day2/{actorId}/need", 1_560 + offset,
                LivingWorldSchedulePhase.Needs, LivingWorldScheduledKind.FoodNeedIncreased, actorId, 250);
            yield return Event($"day2/{actorId}/decision", 1_620 + offset,
                LivingWorldSchedulePhase.DecisionBarrier, LivingWorldScheduledKind.ActorDecision, actorId);
        }
    }

    private static LivingWorldScheduledEvent Event(
        string id,
        long minute,
        LivingWorldSchedulePhase phase,
        LivingWorldScheduledKind kind,
        string actorId,
        int value = 0) => new(id, minute, phase, kind, actorId, value);

    private static string ActorId(int index) => $"npc.population.{index:D4}";
}
