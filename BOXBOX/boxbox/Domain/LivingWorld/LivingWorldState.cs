using System.Collections.ObjectModel;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;

namespace Boxbox.Domain.LivingWorld;

public enum EmploymentStatus
{
    Employed,
    Unemployed
}

public sealed record SimulatedActorState
{
    public SimulatedActorState(
        string actorId,
        long cashCents,
        int foodNeed,
        EmploymentStatus employment,
        long wageCents,
        long debtCents,
        long maximumDebtCents,
        IEnumerable<string>? knownFactIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        if (cashCents < 0 || wageCents < 0 || debtCents < 0 || maximumDebtCents < debtCents)
        {
            throw new ArgumentOutOfRangeException(nameof(cashCents));
        }

        if (foodNeed is < 0 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(foodNeed));
        }

        ActorId = actorId;
        CashCents = cashCents;
        FoodNeed = foodNeed;
        Employment = employment;
        WageCents = wageCents;
        DebtCents = debtCents;
        MaximumDebtCents = maximumDebtCents;
        KnownFactIds = Array.AsReadOnly((knownFactIds ?? []).Distinct(StringComparer.Ordinal).ToArray());
    }

    public string ActorId { get; }
    public long CashCents { get; }
    public int FoodNeed { get; }
    public EmploymentStatus Employment { get; }
    public long WageCents { get; }
    public long DebtCents { get; }
    public long MaximumDebtCents { get; }
    public IReadOnlyList<string> KnownFactIds { get; }
}

public sealed class LivingWorldState
{
    private readonly IReadOnlyDictionary<string, SimulatedActorState> _actors;
    private readonly IReadOnlyDictionary<string, CompetitorMarketState> _competitors;
    private readonly IReadOnlySet<string> _completedCommandIds;

    public LivingWorldState(
        IEnumerable<SimulatedActorState> actors,
        StoreState store,
        long employerCashCents,
        long bankCashCents,
        IEnumerable<string>? completedCommandIds = null,
        IEnumerable<CompetitorMarketState>? competitors = null,
        long externalMarketCashCents = 0)
    {
        ArgumentNullException.ThrowIfNull(actors);
        ArgumentNullException.ThrowIfNull(store);
        if (employerCashCents < 0 || bankCashCents < 0 || externalMarketCashCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employerCashCents));
        }

        var actorMap = actors.ToDictionary(item => item.ActorId, StringComparer.Ordinal);
        if (actorMap.Count == 0)
        {
            throw new ArgumentException("A living world requires at least one actor.", nameof(actors));
        }

        _actors = new ReadOnlyDictionary<string, SimulatedActorState>(actorMap);
        var competitorMap = (competitors ?? [])
            .ToDictionary(item => item.CompetitorId, StringComparer.Ordinal);
        if (competitorMap.ContainsKey(store.ShopId))
        {
            throw new ArgumentException("A competitor cannot reuse the player store id.", nameof(competitors));
        }

        _competitors = new ReadOnlyDictionary<string, CompetitorMarketState>(competitorMap);
        _completedCommandIds = new HashSet<string>(completedCommandIds ?? [], StringComparer.Ordinal);
        Store = store;
        EmployerCashCents = employerCashCents;
        BankCashCents = bankCashCents;
        ExternalMarketCashCents = externalMarketCashCents;
        LivingWorldInvariantRegistry.Validate(this);
    }

    public IReadOnlyDictionary<string, SimulatedActorState> Actors => _actors;
    public StoreState Store { get; }
    public IReadOnlyDictionary<string, CompetitorMarketState> Competitors => _competitors;
    public long EmployerCashCents { get; }
    public long BankCashCents { get; }
    public long ExternalMarketCashCents { get; }
    public IReadOnlySet<string> CompletedCommandIds => _completedCommandIds;
    public long TotalCashCents => checked(
        Store.CashBalanceCents + EmployerCashCents + BankCashCents + ExternalMarketCashCents +
        Actors.Values.Sum(item => item.CashCents));
}

public static class LivingWorldInvariantRegistry
{
    public static void Validate(LivingWorldState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Store.CashBalanceCents < 0 || state.EmployerCashCents < 0 ||
            state.BankCashCents < 0 || state.ExternalMarketCashCents < 0)
        {
            throw new InvalidOperationException("world.invariant.cash_non_negative");
        }

        foreach (var actor in state.Actors.Values)
        {
            if (actor.CashCents < 0 || actor.DebtCents < 0 || actor.DebtCents > actor.MaximumDebtCents)
            {
                throw new InvalidOperationException("world.invariant.actor_finances_valid");
            }

            if (actor.FoodNeed is < 0 or > 1000)
            {
                throw new InvalidOperationException("world.invariant.need_bounded");
            }
        }
    }

    public static void ValidateTransition(LivingWorldState before, LivingWorldState after)
    {
        Validate(after);
        if (before.TotalCashCents != after.TotalCashCents)
        {
            throw new InvalidOperationException("world.invariant.cash_conserved");
        }
    }
}
