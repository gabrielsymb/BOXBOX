using Boxbox.AI.Contracts;
using Boxbox.AI.Customers;
using Boxbox.AI.Randomness;
using Boxbox.Application.Customers;
using Boxbox.Application.Market;
using Boxbox.Domain.Catalog;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.LivingWorld;
using Boxbox.Domain.Market;

namespace Boxbox.Application.LivingWorld;

/// <summary>
/// Bridges a persistent NPC need to the existing shared market. The market
/// chooses and validates an outcome on an immutable affordability view; the
/// living-world transaction then commits buyer and seller together.
/// </summary>
public sealed class LivingWorldMarketPurchaseService
{
    private static readonly ProductCatalog Catalog = new(
        [new ProductDefinition("meal", "essentials")]);

    private static readonly CustomerPreferenceProfile EssentialsProfile = new(
        "living-world-essentials",
        priceWeight: 420,
        queueWeight: 100,
        convenienceWeight: 180,
        reputationWeight: 140,
        familiarityWeight: 160);

    private readonly LivingWorldTransactionService _transactions;

    public LivingWorldMarketPurchaseService(LivingWorldTransactionService transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        _transactions = transactions;
    }

    public LivingWorldCommandResult Execute(
        LivingWorldState state,
        string decisionId,
        string actorId,
        ulong choiceSeed)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        if (!state.Actors.TryGetValue(actorId, out var actor))
        {
            return LivingWorldCommandResult.Rejected(state, LivingWorldRejectionReason.UnknownActor);
        }

        var opportunityId = $"{decisionId}/meal-opportunity";
        var opportunity = new MarketPurchaseOpportunity(
            opportunityId,
            "essentials",
            "meal",
            quantity: 1,
            spendingLimitCents: actor.CashCents);
        var competitors = state.Competitors.Values.ToArray();
        var market = new SharedMarketState(Catalog, state.Store, competitors, [opportunity]);
        var profiles = competitors.Select(item => new CompetitorProfile(
            item.CompetitorId,
            $"competitor.{item.CompetitorId}.name",
            ["essentials"],
            new CompetitorPublicAttributes("neighborhood", convenience: 560, reputation: 540)))
            .ToArray();
        var observations = competitors
            .SelectMany(item => item.Offers
                .Where(offer => offer.Targets(opportunity))
                .Select(offer => new CompetitorOfferObservation(
                    $"{opportunityId}/observe/{item.CompetitorId}/{offer.OfferId}",
                    opportunityId,
                    item.CompetitorId,
                    offer.OfferId,
                    baseUtility: 0,
                    new ChoiceFeatures(
                        PriceAttractiveness(offer.UnitPriceCents, PlayerPrice(state)),
                        QueueComfort: 570,
                        Convenience: 560,
                        Reputation: 540,
                        Familiarity: 500))))
            .ToArray();
        var playerFeatures = new ChoiceFeatures(
            PriceAttractiveness(PlayerPrice(state), MinimumRivalPrice(state)),
            QueueComfort: 620,
            Convenience: 650,
            Reputation: 560,
            Familiarity: 520);
        var customer = new CustomerChoiceSession(
            actorId,
            EssentialsProfile,
            new SoftmaxCustomerChoiceEngine(temperature: 95),
            learningRate: 160);
        var service = new SharedMarketPurchaseService(
            new AdmissibleMarketOfferGenerator(),
            new CustomerPurchaseService(noPurchaseBaseUtility: 520 - actor.FoodNeed / 4));
        var marketResult = service.Execute(
            customer,
            market,
            opportunityId,
            decisionId,
            playerFeatures,
            profiles,
            observations,
            new SeededChoiceRandomSource(choiceSeed));

        if (marketResult.Status != SharedMarketPurchaseStatus.Resolved || marketResult.Resolution is null)
        {
            return LivingWorldCommandResult.Rejected(
                state,
                LivingWorldRejectionReason.MarketResolutionRejected);
        }

        return _transactions.CompleteMarketPurchase(
            state,
            decisionId,
            actorId,
            marketResult.Resolution);
    }

    private static long PlayerPrice(LivingWorldState state) =>
        state.Store.TryGetUnitPriceCents("meal", out var priceCents) ? priceCents : 1_000;

    private static long MinimumRivalPrice(LivingWorldState state) => state.Competitors.Values
        .Where(item => item.IsOpen && item.AvailableServiceCapacity > 0)
        .SelectMany(item => item.Offers)
        .Where(item => item.IsActive && item.AvailableQuantity > 0 &&
            string.Equals(item.CategoryId, "essentials", StringComparison.Ordinal) &&
            (item.ProductId is null || string.Equals(item.ProductId, "meal", StringComparison.Ordinal)))
        .Select(item => item.UnitPriceCents)
        .DefaultIfEmpty(PlayerPrice(state))
        .Min();

    private static int PriceAttractiveness(long ownPriceCents, long comparisonPriceCents)
    {
        var ratio = (comparisonPriceCents * 500L) / ownPriceCents;
        return (int)Math.Clamp(ratio, 0, 1000);
    }
}
