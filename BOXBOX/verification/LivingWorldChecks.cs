using Boxbox.Application.LivingWorld;
using Boxbox.Domain.LivingWorld;
using Boxbox.Domain.Market;
using Boxbox.Persistence;
using static Boxbox.Verification.VerificationAssert;

namespace Boxbox.Verification;

internal static class LivingWorldChecks
{
    internal static void CausalActorTrajectoryChangesEconomy()
    {
        var simulation = LivingWorldPrototypeScenario.Create(20261119);
        var openingCash = simulation.State.TotalCashCents;

        var result = simulation.AdvanceTo(LivingWorldPrototypeScenario.FinalMinute);

        Require(result.State.TotalCashCents == openingCash,
            "Salary, purchases and loans must transfer rather than create cash.");
        Require(result.Events.Any(item => item.ReasonKey == "world.employment.job_lost" && item.ActorId == "npc.lia"),
            "The employer closure must cause Lia's job loss.");
        Require(result.Events.Any(item => item.MarketOutcome is not null),
            "Persistent needs must create shared-market resolutions.");
        Require(result.Events.Any(item => item.ActionId is "action.borrow_for_essentials" or "action.seek_employment"),
            "Job loss and depleted cash must cause a new recovery decision.");
        Require(result.State.Store.StockByProduct["meal"] < 8 || result.State.ExternalMarketCashCents > 0,
            "NPC decisions must change BOXBOX or the summarized rival economy.");
    }

    internal static void ClosedStoreCannotBeChosenByNpcPolicy()
    {
        var simulation = LivingWorldPrototypeScenario.Create(77, playerStoreOpen: false);

        var result = simulation.AdvanceTo(LivingWorldPrototypeScenario.FinalMinute);

        Require(result.State.Store.StockByProduct["meal"] == 8 && result.State.Store.CashBalanceCents == 5_000,
            "A closed store cannot receive NPC purchases.");
        Require(result.Events.All(item => item.MarketOutcome != MarketPurchaseOutcomeKind.PlayerStoreSale),
            "A closed BOXBOX must never be selected while the rival remains independently admissible.");
    }

    internal static void RejectedWorldCommandIsAtomic()
    {
        var state = LivingWorldPrototypeScenario.Create(1, playerStoreOpen: false).State;
        var transactions = new LivingWorldTransactionService();

        var result = transactions.Execute(state, new PurchaseMealCommand("purchase/closed", "npc.lia"));

        Require(!result.Completed && result.RejectionReason == LivingWorldRejectionReason.StoreClosed,
            "A purchase against a closed store must be rejected with a reason.");
        Require(ReferenceEquals(state, result.ResultingState),
            "A rejected world command must preserve the exact snapshot.");
    }

    internal static void IncrementalAndCatchUpAdvanceMatch()
    {
        var incremental = LivingWorldPrototypeScenario.Create(9123);
        foreach (var minute in new long[] { 180, 1_620, 3_120, LivingWorldPrototypeScenario.FinalMinute })
        {
            incremental.AdvanceTo(minute);
        }

        var catchUp = LivingWorldPrototypeScenario.Create(9123);
        catchUp.AdvanceTo(LivingWorldPrototypeScenario.FinalMinute);

        Require(Fingerprint(incremental.CreateSnapshot()) == Fingerprint(catchUp.CreateSnapshot()),
            "Continuous and catch-up processing must produce the same canonical world.");
    }

    internal static void JsonSaveResumesWithoutDuplicatingEffects()
    {
        var direct = LivingWorldPrototypeScenario.Create(4455);
        direct.AdvanceTo(LivingWorldPrototypeScenario.FinalMinute);

        var interrupted = LivingWorldPrototypeScenario.Create(4455);
        interrupted.AdvanceTo(1_620);
        var directory = Path.Combine(Path.GetTempPath(), $"boxbox-world-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "snapshot.json");
        try
        {
            var store = new JsonLivingWorldSnapshotStore(path);
            store.Save(interrupted.CreateSnapshot());
            var restored = LivingWorldSimulation.Restore(store.Load());
            restored.AdvanceTo(LivingWorldPrototypeScenario.FinalMinute);

            Require(Fingerprint(restored.CreateSnapshot()) == Fingerprint(direct.CreateSnapshot()),
                "A persisted snapshot must resume the same story without duplicate effects.");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    internal static void PlayerStoreInterventionChangesNpcConsequences()
    {
        var open = LivingWorldPrototypeScenario.Create(1, playerStoreOpen: true);
        var closed = LivingWorldPrototypeScenario.Create(1, playerStoreOpen: true);
        closed.ApplyPlayerStoreCommand("player/close-store", isOpen: false);
        open.AdvanceTo(180);
        closed.AdvanceTo(180);

        Require(open.EventLog.Any(item => item.MarketOutcome == MarketPurchaseOutcomeKind.PlayerStoreSale) &&
                closed.EventLog.All(item => item.MarketOutcome != MarketPurchaseOutcomeKind.PlayerStoreSale) &&
                Fingerprint(open.CreateSnapshot()) != Fingerprint(closed.CreateSnapshot()),
            "Opening BOXBOX must add a real market alternative capable of changing a seeded trajectory.");
    }

    internal static void SharedMarketCanResolveEveryTerminalAlternative()
    {
        var outcomes = new HashSet<MarketPurchaseOutcomeKind>();
        foreach (var seed in new ulong[] { 1, 9 })
        {
            var simulation = LivingWorldPrototypeScenario.Create(seed);
            simulation.AdvanceTo(180);
            foreach (var outcome in simulation.EventLog
                         .Where(item => item.MarketOutcome is not null)
                         .Select(item => item.MarketOutcome!.Value))
            {
                outcomes.Add(outcome);
            }
        }

        Require(outcomes.Contains(MarketPurchaseOutcomeKind.PlayerStoreSale),
            "The living NPC must be able to choose BOXBOX.");
        Require(outcomes.Contains(MarketPurchaseOutcomeKind.CompetitorSale),
            "The living NPC must be able to choose the admissible rival.");
        Require(outcomes.Contains(MarketPurchaseOutcomeKind.ChoseNotToPurchase),
            "The living NPC must retain the external option not to buy.");
    }

    internal static void RivalSalePersistsAndConservesBuyerMoney()
    {
        var chosen = LivingWorldPrototypeScenario.Create(1);
        chosen.AdvanceTo(180);
        Require(chosen.EventLog.Any(item => item.MarketOutcome == MarketPurchaseOutcomeKind.CompetitorSale),
            "Golden seed 1 must retain its rival-sale replay.");
        var rival = chosen.State.Competitors["rival-market"];
        Require(chosen.State.TotalCashCents == 21_100,
            "A rival sale must transfer buyer cash into the external market reservoir.");
        var rivalEvents = chosen.EventLog.Count(item => item.MarketOutcome == MarketPurchaseOutcomeKind.CompetitorSale);
        var playerEvents = chosen.EventLog.Count(item => item.MarketOutcome == MarketPurchaseOutcomeKind.PlayerStoreSale);
        Require(chosen.State.Store.CashBalanceCents == 5_000 + playerEvents * 500 &&
                chosen.State.Store.StockByProduct["meal"] == 8 - playerEvents,
            "Only player-store outcomes may mutate BOXBOX cash or stock.");
        Require(chosen.State.ExternalMarketCashCents > 0 &&
                chosen.State.ExternalMarketCashCents == rivalEvents * 450 &&
                rival.Offers.Single(item => item.OfferId == "rival-meal").AvailableQuantity == 20 - rivalEvents,
            "The rival receipt and summarized availability must persist in the world state.");
    }

    internal static void PersistentPopulationScaleReplaysDeterministically()
    {
        const int actorCount = LivingWorldPopulationScaleScenario.DefaultActorCount;
        const ulong seed = 128_2026;
        var incremental = LivingWorldPopulationScaleScenario.Create(seed, actorCount);
        var openingCash = incremental.State.TotalCashCents;
        incremental.AdvanceTo(300);
        incremental.AdvanceTo(1_200);
        incremental.AdvanceTo(LivingWorldPopulationScaleScenario.FinalMinute);

        var catchUp = LivingWorldPopulationScaleScenario.Create(seed, actorCount);
        catchUp.AdvanceTo(LivingWorldPopulationScaleScenario.FinalMinute);

        Require(incremental.State.Actors.Count == actorCount,
            "The population fixture must retain every persistent individual.");
        Require(incremental.EventLog.Select(item => item.ActorId).Distinct(StringComparer.Ordinal).Count() == actorCount,
            "Every persistent actor must participate in the scheduled world.");
        Require(incremental.State.TotalCashCents == openingCash,
            "Population scale must not create or destroy money.");
        Require(ScaleFingerprint(incremental.CreateSnapshot()) == ScaleFingerprint(catchUp.CreateSnapshot()),
            "A 128-actor world must replay identically in incremental and catch-up processing.");
    }

    private static string ScaleFingerprint(LivingWorldSnapshot snapshot) => string.Join(
        "|",
        snapshot.CurrentMinute,
        snapshot.State.TotalCashCents,
        snapshot.State.Store.CashBalanceCents,
        snapshot.State.Store.StockByProduct["meal"],
        snapshot.State.ExternalMarketCashCents,
        string.Join(",", snapshot.State.Actors.Values
            .OrderBy(item => item.ActorId, StringComparer.Ordinal)
            .Select(item => $"{item.ActorId}:{item.CashCents}:{item.FoodNeed}:{item.Employment}:{item.DebtCents}")),
        string.Join(",", snapshot.ProcessedEventIds.Order(StringComparer.Ordinal)),
        string.Join(",", snapshot.EventLog.Select(item =>
            $"{item.StableId}:{item.ActorId}:{item.ActionId}:{item.MarketOutcome}:{item.SelectedShopId}")));

    private static string Fingerprint(LivingWorldSnapshot snapshot) => string.Join(
        "|",
        snapshot.RulesetVersion,
        snapshot.Seed,
        snapshot.CurrentMinute,
        snapshot.State.Store.IsOpen,
        snapshot.State.Store.CashBalanceCents,
        snapshot.State.Store.StockByProduct["meal"],
        snapshot.State.EmployerCashCents,
        snapshot.State.BankCashCents,
        snapshot.State.ExternalMarketCashCents,
        string.Join(",", snapshot.State.Actors.Values
            .OrderBy(item => item.ActorId, StringComparer.Ordinal)
            .Select(item => $"{item.ActorId}:{item.CashCents}:{item.FoodNeed}:{item.Employment}:{item.DebtCents}:{string.Join("+", item.KnownFactIds)}")),
        string.Join(",", snapshot.ProcessedEventIds.Order(StringComparer.Ordinal)),
        string.Join(",", snapshot.State.Competitors.Values
            .OrderBy(item => item.CompetitorId, StringComparer.Ordinal)
            .Select(item => $"{item.CompetitorId}:{item.AvailableServiceCapacity}:{string.Join("+", item.Offers.Select(offer => $"{offer.OfferId}={offer.AvailableQuantity}"))}")),
        string.Join(",", snapshot.EventLog.Select(item =>
            $"{item.StableId}:{item.ReasonKey}:{item.ActionId}:{item.CashDeltaCents}:{item.NeedDelta}:{item.StoreStockDelta}:{item.MarketOutcome}:{item.SelectedShopId}")));
}
