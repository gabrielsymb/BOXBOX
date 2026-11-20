using Boxbox.AI.Contracts;
using Boxbox.AI.Customers;
using Boxbox.AI.Randomness;
using Boxbox.Application.Market;
using Boxbox.Domain.Market;

namespace Boxbox.Application.MiniTurn;

/// <summary>
/// Small headless scheduling slice. It owns simulation time and coordinates
/// existing authoritative market transactions; rendering only consumes the
/// returned events.
/// </summary>
public sealed class MiniTurnSimulation
{
    private readonly ulong _baseSeed;
    private readonly ICustomerChoiceEngine _choiceEngine;
    private readonly int _experienceLearningRate;
    private readonly SharedMarketPurchaseService _purchaseService;
    private readonly IReadOnlyList<CompetitorProfile> _competitorProfiles;
    private readonly IReadOnlyList<ScheduledStep> _schedule;
    private readonly HashSet<string> _processedStepIds = new(StringComparer.Ordinal);
    private SharedMarketState _marketState;

    public MiniTurnSimulation(
        ulong baseSeed,
        long startMinute,
        SharedMarketState initialMarketState,
        IEnumerable<MiniTurnCustomerPlan> customerPlans,
        IEnumerable<CompetitorProfile> competitorProfiles,
        ICustomerChoiceEngine choiceEngine,
        int experienceLearningRate,
        SharedMarketPurchaseService purchaseService)
    {
        ArgumentNullException.ThrowIfNull(initialMarketState);
        ArgumentNullException.ThrowIfNull(customerPlans);
        ArgumentNullException.ThrowIfNull(competitorProfiles);
        ArgumentNullException.ThrowIfNull(choiceEngine);
        ArgumentNullException.ThrowIfNull(purchaseService);
        if (startMinute < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startMinute));
        }

        if (experienceLearningRate is < 0 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(experienceLearningRate));
        }

        var plans = customerPlans.ToArray();
        ValidatePlans(initialMarketState, plans);
        _baseSeed = baseSeed;
        CurrentMinute = startMinute;
        _marketState = initialMarketState;
        _competitorProfiles = Array.AsReadOnly(competitorProfiles.ToArray());
        _choiceEngine = choiceEngine;
        _experienceLearningRate = experienceLearningRate;
        _purchaseService = purchaseService;
        _schedule = BuildSchedule(plans);
    }

    public long CurrentMinute { get; private set; }
    public SharedMarketState MarketState => _marketState;
    public bool IsComplete => _processedStepIds.Count == _schedule.Count;

    public MiniTurnAdvanceResult AdvanceTo(long targetMinute)
    {
        if (targetMinute < CurrentMinute)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetMinute),
                "Simulation time is monotonic.");
        }

        var emitted = new List<MiniTurnEvent>();
        foreach (var step in _schedule)
        {
            if (step.Minute > targetMinute || _processedStepIds.Contains(step.StableId))
            {
                continue;
            }

            emitted.Add(step.Kind == MiniTurnEventKind.CustomerArrived
                ? ProjectArrival(step)
                : ResolveCustomer(step));
            _processedStepIds.Add(step.StableId);
        }

        CurrentMinute = targetMinute;
        return new MiniTurnAdvanceResult(
            CurrentMinute,
            _marketState,
            Array.AsReadOnly(emitted.ToArray()));
    }

    private MiniTurnEvent ProjectArrival(ScheduledStep step)
    {
        var opportunity = _marketState.Opportunities[step.Plan.OpportunityId].Opportunity;
        var productId = opportunity.ProductId
            ?? throw new InvalidOperationException("The mini-turn requires product-specific opportunities.");
        return new MiniTurnEvent(
            step.StableId,
            step.Minute,
            MiniTurnEventKind.CustomerArrived,
            step.Plan.CustomerId,
            productId,
            null,
            null,
            opportunity.Quantity,
            null,
            _marketState.PlayerStore.CashBalanceCents,
            _marketState.PlayerStore.StockByProduct.GetValueOrDefault(productId));
    }

    private MiniTurnEvent ResolveCustomer(ScheduledStep step)
    {
        var plan = step.Plan;
        var customer = new CustomerChoiceSession(
            plan.CustomerId,
            plan.PreferenceProfile,
            _choiceEngine,
            _experienceLearningRate);
        var result = _purchaseService.Execute(
            customer,
            _marketState,
            plan.OpportunityId,
            plan.DecisionId,
            plan.PlayerStorePerceivedFeatures,
            _competitorProfiles,
            plan.CompetitorObservations,
            new SeededChoiceRandomSource(DeriveEventSeed(_baseSeed, step.StableId)));
        if (result.Status != SharedMarketPurchaseStatus.Resolved || result.Resolution is null)
        {
            throw new InvalidOperationException(
                $"Mini-turn resolution '{step.StableId}' was rejected: {result.RejectionReason}.");
        }

        _marketState = result.ResultingMarketState;
        var resolution = result.Resolution;
        var productId = resolution.ProductId;
        return new MiniTurnEvent(
            step.StableId,
            step.Minute,
            MiniTurnEventKind.CustomerResolved,
            plan.CustomerId,
            productId,
            resolution.Outcome,
            resolution.ShopId,
            resolution.Quantity,
            resolution.TotalCents,
            _marketState.PlayerStore.CashBalanceCents,
            _marketState.PlayerStore.StockByProduct.GetValueOrDefault(productId));
    }

    private static IReadOnlyList<ScheduledStep> BuildSchedule(IEnumerable<MiniTurnCustomerPlan> plans) =>
        Array.AsReadOnly(plans
            .SelectMany(plan => new[]
            {
                new ScheduledStep(
                    $"{plan.OpportunityId}/arrival",
                    plan.ArrivalMinute,
                    MiniTurnEventKind.CustomerArrived,
                    plan),
                new ScheduledStep(
                    $"{plan.OpportunityId}/resolution",
                    plan.DecisionMinute,
                    MiniTurnEventKind.CustomerResolved,
                    plan)
            })
            .OrderBy(step => step.Minute)
            .ThenBy(step => step.Kind)
            .ThenBy(step => step.StableId, StringComparer.Ordinal)
            .ToArray());

    private static void ValidatePlans(
        SharedMarketState initialMarketState,
        IReadOnlyCollection<MiniTurnCustomerPlan> plans)
    {
        var customerIds = new HashSet<string>(StringComparer.Ordinal);
        var opportunityIds = new HashSet<string>(StringComparer.Ordinal);
        var decisionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var plan in plans)
        {
            ArgumentNullException.ThrowIfNull(plan);
            if (!customerIds.Add(plan.CustomerId) ||
                !opportunityIds.Add(plan.OpportunityId) ||
                !decisionIds.Add(plan.DecisionId))
            {
                throw new ArgumentException(
                    "Mini-turn customer, opportunity and decision ids must be unique.",
                    nameof(plans));
            }

            if (!initialMarketState.Opportunities.TryGetValue(plan.OpportunityId, out var record) ||
                record.Opportunity.ProductId is null)
            {
                throw new ArgumentException(
                    $"Mini-turn plan references an unknown or category-only opportunity: {plan.OpportunityId}.",
                    nameof(plans));
            }
        }
    }

    private static ulong DeriveEventSeed(ulong baseSeed, string stableId)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;
        foreach (var character in stableId)
        {
            hash ^= character;
            hash *= prime;
        }

        return baseSeed ^ hash;
    }

    private sealed record ScheduledStep(
        string StableId,
        long Minute,
        MiniTurnEventKind Kind,
        MiniTurnCustomerPlan Plan);
}
