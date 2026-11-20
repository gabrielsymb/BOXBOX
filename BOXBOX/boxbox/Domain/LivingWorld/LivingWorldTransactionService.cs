using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;

namespace Boxbox.Domain.LivingWorld;

public sealed class LivingWorldTransactionService
{
    public LivingWorldCommandResult CompleteMarketPurchase(
        LivingWorldState state,
        string commandId,
        string actorId,
        MarketOpportunityResolved resolution)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentNullException.ThrowIfNull(resolution);
        if (state.CompletedCommandIds.Contains(commandId))
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.DuplicateCommand);
        }

        if (!state.Actors.TryGetValue(actorId, out var actor))
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.UnknownActor);
        }

        if (!string.Equals(resolution.DecisionId, commandId, StringComparison.Ordinal) ||
            !string.Equals(resolution.CustomerId, actorId, StringComparison.Ordinal) ||
            !string.Equals(resolution.ProductId, "meal", StringComparison.Ordinal) ||
            resolution.Quantity != 1)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.MarketResolutionMismatch);
        }

        var command = new PurchaseMealCommand(commandId, actorId);
        if (resolution.Outcome is MarketPurchaseOutcomeKind.ChoseNotToPurchase or
            MarketPurchaseOutcomeKind.DemandUnserved)
        {
            return Commit(
                state,
                command,
                state.Actors.Values,
                state.Store,
                state.EmployerCashCents,
                state.BankCashCents,
                new LivingWorldEffect(
                    LivingWorldEffectKind.MarketPurchaseResolved,
                    actorId,
                    resolution.Outcome == MarketPurchaseOutcomeKind.DemandUnserved
                        ? "world.market.demand_unserved"
                        : "world.market.no_purchase",
                    MarketOutcome: resolution.Outcome));
        }

        if (actor.FoodNeed <= 0 || resolution.TotalCents is null || resolution.TotalCents <= 0 ||
            actor.CashCents < resolution.TotalCents)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.InsufficientActorCash);
        }

        var totalCents = resolution.TotalCents.Value;
        var nextActor = new SimulatedActorState(
            actor.ActorId,
            actor.CashCents - totalCents,
            Math.Max(0, actor.FoodNeed - 650),
            actor.Employment,
            actor.WageCents,
            actor.DebtCents,
            actor.MaximumDebtCents,
            actor.KnownFactIds);
        var nextStore = state.Store;
        var nextCompetitors = state.Competitors.Values.ToArray();
        var externalMarketCashCents = state.ExternalMarketCashCents;
        var storeStockDelta = 0;

        if (resolution.Outcome == MarketPurchaseOutcomeKind.PlayerStoreSale)
        {
            var sale = state.Store.CompleteSale(new SaleCommand(
                resolution.DecisionId,
                actorId,
                resolution.ShopId!,
                resolution.ProductId,
                resolution.Quantity));
            if (!sale.Completed || sale.Sale is null || sale.Sale.TotalCents != totalCents)
            {
                return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.MarketResolutionMismatch);
            }

            nextStore = sale.ResultingState;
            storeStockDelta = -resolution.Quantity;
        }
        else if (resolution.Outcome == MarketPurchaseOutcomeKind.CompetitorSale)
        {
            if (!state.Competitors.TryGetValue(resolution.ShopId!, out var competitor))
            {
                return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.MarketResolutionMismatch);
            }

            var sale = competitor.CompleteSale(new CompetitorSaleCommand(
                resolution.DecisionId,
                actorId,
                competitor.CompetitorId,
                resolution.OfferId!,
                "essentials",
                resolution.ProductId,
                resolution.Quantity));
            if (!sale.Completed || sale.Sale is null || sale.Sale.TotalCents != totalCents)
            {
                return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.MarketResolutionMismatch);
            }

            nextCompetitors = state.Competitors.Values
                .Select(item => string.Equals(item.CompetitorId, competitor.CompetitorId, StringComparison.Ordinal)
                    ? sale.ResultingState
                    : item)
                .ToArray();
            externalMarketCashCents = checked(externalMarketCashCents + totalCents);
        }
        else
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.MarketResolutionMismatch);
        }

        return Commit(
            state,
            command,
            ReplaceActor(state, actorId, nextActor),
            nextStore,
            state.EmployerCashCents,
            state.BankCashCents,
            new LivingWorldEffect(
                LivingWorldEffectKind.MarketPurchaseResolved,
                actorId,
                "world.market.meal_purchased",
                -totalCents,
                nextActor.FoodNeed - actor.FoodNeed,
                storeStockDelta,
                resolution.Outcome,
                resolution.ShopId),
            nextCompetitors,
            externalMarketCashCents);
    }

    public LivingWorldCommandResult Execute(LivingWorldState state, LivingWorldCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CommandId);
        if (state.CompletedCommandIds.Contains(command.CommandId))
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.DuplicateCommand);
        }

        if (command is SetStoreOpenCommand storeCommand)
        {
            return Commit(state, command, state.Actors.Values,
                storeCommand.IsOpen ? state.Store.Open() : state.Store.Close(),
                state.EmployerCashCents, state.BankCashCents,
                new LivingWorldEffect(
                    storeCommand.IsOpen ? LivingWorldEffectKind.StoreOpened : LivingWorldEffectKind.StoreClosed,
                    command.ActorId,
                    storeCommand.IsOpen ? "world.store.opened" : "world.store.closed"));
        }

        if (!state.Actors.TryGetValue(command.ActorId, out var actor))
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.UnknownActor);
        }

        return command switch
        {
            IncreaseFoodNeedCommand increase => IncreaseNeed(state, actor, increase),
            PaySalaryCommand salary => PaySalary(state, actor, salary),
            LoseJobCommand loseJob => LoseJob(state, actor, loseJob),
            PurchaseMealCommand purchase => PurchaseMeal(state, actor, purchase),
            BorrowForEssentialsCommand borrow => Borrow(state, actor, borrow),
            SeekEmploymentCommand seek => SeekEmployment(state, actor, seek),
            _ => LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.UnsupportedCommand)
        };
    }

    private static LivingWorldCommandResult IncreaseNeed(
        LivingWorldState state,
        SimulatedActorState actor,
        IncreaseFoodNeedCommand command)
    {
        if (command.Amount <= 0)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.InvalidAmount);
        }

        var nextNeed = Math.Min(1000, checked(actor.FoodNeed + command.Amount));
        return CommitActor(state, command, actor,
            new SimulatedActorState(actor.ActorId, actor.CashCents, nextNeed, actor.Employment,
                actor.WageCents, actor.DebtCents, actor.MaximumDebtCents, actor.KnownFactIds),
            new LivingWorldEffect(LivingWorldEffectKind.NeedIncreased, actor.ActorId,
                "world.need.food.increased", NeedDelta: nextNeed - actor.FoodNeed));
    }

    private static LivingWorldCommandResult PaySalary(
        LivingWorldState state,
        SimulatedActorState actor,
        PaySalaryCommand command)
    {
        if (actor.Employment != EmploymentStatus.Employed)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.ActorNotEmployed);
        }

        if (state.EmployerCashCents < actor.WageCents)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.InsufficientEmployerCash);
        }

        var nextActor = new SimulatedActorState(actor.ActorId, checked(actor.CashCents + actor.WageCents),
            actor.FoodNeed, actor.Employment, actor.WageCents, actor.DebtCents,
            actor.MaximumDebtCents, actor.KnownFactIds);
        return CommitActor(state, command, actor, nextActor,
            new LivingWorldEffect(LivingWorldEffectKind.SalaryPaid, actor.ActorId,
                "world.employment.salary_paid", actor.WageCents),
            employerCashCents: state.EmployerCashCents - actor.WageCents);
    }

    private static LivingWorldCommandResult LoseJob(
        LivingWorldState state,
        SimulatedActorState actor,
        LoseJobCommand command)
    {
        if (actor.Employment != EmploymentStatus.Employed)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.ActorNotEmployed);
        }

        var facts = actor.KnownFactIds.Append(command.FactId);
        var nextActor = new SimulatedActorState(actor.ActorId, actor.CashCents, actor.FoodNeed,
            EmploymentStatus.Unemployed, actor.WageCents, actor.DebtCents,
            actor.MaximumDebtCents, facts);
        return CommitActor(state, command, actor, nextActor,
            new LivingWorldEffect(LivingWorldEffectKind.JobLost, actor.ActorId,
                "world.employment.job_lost"));
    }

    private static LivingWorldCommandResult PurchaseMeal(
        LivingWorldState state,
        SimulatedActorState actor,
        PurchaseMealCommand command)
    {
        if (!state.Store.TryGetUnitPriceCents("meal", out var mealPriceCents))
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.StoreOutOfStock);
        }

        if (!state.Store.IsOpen)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.StoreClosed);
        }

        if (!state.Store.StockByProduct.TryGetValue("meal", out var stock) || stock <= 0)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.StoreOutOfStock);
        }

        if (actor.CashCents < mealPriceCents)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.InsufficientActorCash);
        }

        if (actor.FoodNeed <= 0)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.NeedNotPresent);
        }

        var nextActor = new SimulatedActorState(actor.ActorId,
            actor.CashCents - mealPriceCents,
            Math.Max(0, actor.FoodNeed - 650), actor.Employment, actor.WageCents,
            actor.DebtCents, actor.MaximumDebtCents, actor.KnownFactIds);
        var sale = state.Store.CompleteSale(new SaleCommand(
            command.CommandId,
            actor.ActorId,
            state.Store.ShopId,
            "meal",
            quantity: 1));
        if (!sale.Completed || sale.Sale is null)
        {
            throw new InvalidOperationException(
                $"Admissible living-world sale was rejected: {sale.RejectionReason}.");
        }

        return Commit(state, command,
            ReplaceActor(state, actor.ActorId, nextActor), sale.ResultingState,
            state.EmployerCashCents, state.BankCashCents,
            new LivingWorldEffect(LivingWorldEffectKind.MealPurchased, actor.ActorId,
                "world.need.food.purchased", -mealPriceCents,
                nextActor.FoodNeed - actor.FoodNeed, -1));
    }

    private static LivingWorldCommandResult Borrow(
        LivingWorldState state,
        SimulatedActorState actor,
        BorrowForEssentialsCommand command)
    {
        if (command.AmountCents <= 0)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.InvalidAmount);
        }

        if (checked(actor.DebtCents + command.AmountCents) > actor.MaximumDebtCents)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.DebtLimitExceeded);
        }

        if (state.BankCashCents < command.AmountCents)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.InsufficientBankCash);
        }

        var nextActor = new SimulatedActorState(actor.ActorId,
            checked(actor.CashCents + command.AmountCents), actor.FoodNeed, actor.Employment,
            actor.WageCents, checked(actor.DebtCents + command.AmountCents),
            actor.MaximumDebtCents, actor.KnownFactIds);
        return CommitActor(state, command, actor, nextActor,
            new LivingWorldEffect(LivingWorldEffectKind.LoanReceived, actor.ActorId,
                "world.finance.loan_received", command.AmountCents),
            bankCashCents: state.BankCashCents - command.AmountCents);
    }

    private static LivingWorldCommandResult SeekEmployment(
        LivingWorldState state,
        SimulatedActorState actor,
        SeekEmploymentCommand command)
    {
        if (actor.Employment != EmploymentStatus.Unemployed)
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.ActorAlreadyEmployed);
        }

        var nextActor = command.Succeeded
            ? new SimulatedActorState(actor.ActorId, actor.CashCents, actor.FoodNeed,
                EmploymentStatus.Employed, actor.WageCents, actor.DebtCents,
                actor.MaximumDebtCents, actor.KnownFactIds)
            : actor;
        return CommitActor(state, command, actor, nextActor,
            new LivingWorldEffect(
                command.Succeeded ? LivingWorldEffectKind.EmploymentFound : LivingWorldEffectKind.EmploymentSearchFailed,
                actor.ActorId,
                command.Succeeded ? "world.employment.found" : "world.employment.search_failed"));
    }

    private static LivingWorldCommandResult CommitActor(
        LivingWorldState state,
        LivingWorldCommand command,
        SimulatedActorState previousActor,
        SimulatedActorState nextActor,
        LivingWorldEffect effect,
        long? employerCashCents = null,
        long? bankCashCents = null) => Commit(
            state,
            command,
            ReplaceActor(state, previousActor.ActorId, nextActor),
            state.Store,
            employerCashCents ?? state.EmployerCashCents,
            bankCashCents ?? state.BankCashCents,
            effect);

    private static LivingWorldCommandResult Commit(
        LivingWorldState state,
        LivingWorldCommand command,
        IEnumerable<SimulatedActorState> actors,
        StoreState store,
        long employerCashCents,
        long bankCashCents,
        LivingWorldEffect effect,
        IEnumerable<CompetitorMarketState>? competitors = null,
        long? externalMarketCashCents = null)
    {
        var completed = state.CompletedCommandIds.Append(command.CommandId);
        var next = new LivingWorldState(
            actors,
            store,
            employerCashCents,
            bankCashCents,
            completed,
            competitors ?? state.Competitors.Values,
            externalMarketCashCents ?? state.ExternalMarketCashCents);
        LivingWorldInvariantRegistry.ValidateTransition(state, next);
        return LivingWorldCommandResult.Succeeded(next, effect);
    }

    private static IEnumerable<SimulatedActorState> ReplaceActor(
        LivingWorldState state,
        string actorId,
        SimulatedActorState replacement) => state.Actors.Values
        .Select(item => string.Equals(item.ActorId, actorId, StringComparison.Ordinal) ? replacement : item);
}
