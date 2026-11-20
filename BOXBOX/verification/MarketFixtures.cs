using Boxbox.AI.Contracts;
using Boxbox.AI.Customers;
using Boxbox.Application.Customers;
using Boxbox.Application.Market;
using Boxbox.Domain.Catalog;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;

namespace Boxbox.Verification;

internal static class MarketFixtures
{
    internal static CustomerPurchaseFlowResult ExecuteMarketChoice(
        StoreState store,
        CustomerChoiceSession customer,
        double roll,
        int priceAttractiveness = 600,
        string decisionId = "market-decision",
        string observationId = "a-purchase-at-rival-market") =>
        ExecuteSharedMarketChoice(
            store,
            customer,
            roll,
            priceAttractiveness,
            decisionId,
            observationId).PurchaseFlow
        ?? throw new InvalidOperationException("Expected a completed customer choice.");

    internal static SharedMarketPurchaseResult ExecuteSharedMarketChoice(
        StoreState store,
        CustomerChoiceSession customer,
        double roll,
        int priceAttractiveness = 600,
        string decisionId = "market-decision",
        string observationId = "a-purchase-at-rival-market")
    {
        var opportunity = StandardOpportunity(decisionId);
        const string competitorId = "rival-market";
        var offer = StandardCompetitorOffer("rival-rice");
        var competitorState = StandardCompetitorState(competitorId, offers: [offer]);
        var marketState = StandardMarketState(
            store,
            [competitorState],
            [opportunity]);
        return NewSharedMarketPurchaseService().Execute(
            customer,
            marketState,
            opportunity.OpportunityId,
            decisionId,
            StandardFeatures(),
            [StandardCompetitorProfile(competitorId)],
            [StandardObservation(
                opportunity,
                competitorId,
                offer.OfferId,
                observationId,
                priceAttractiveness)],
            new FixedRandomSource(roll));
    }

    internal static SharedMarketPurchaseService NewSharedMarketPurchaseService() => new(
        new AdmissibleMarketOfferGenerator(),
        new CustomerPurchaseService(noPurchaseBaseUtility: 300));

    internal static ProductCatalog StandardCatalog() => new(
    [
        new ProductDefinition("beans", "grocery"),
        new ProductDefinition("rice", "grocery")
    ]);

    internal static SharedMarketState StandardMarketState(
        StoreState store,
        IEnumerable<CompetitorMarketState> competitors,
        IEnumerable<MarketPurchaseOpportunity> opportunities,
        ProductCatalog? catalog = null) =>
        new(catalog ?? StandardCatalog(), store, competitors, opportunities);

    internal static MarketPurchaseOpportunity StandardOpportunity(string opportunityId) =>
        new(opportunityId, "grocery", "rice", quantity: 1);

    internal static CompetitorProfile StandardCompetitorProfile(
        string competitorId,
        string categoryId = "grocery") =>
        new(
            competitorId,
            $"competitor.{competitorId}.name",
            [categoryId],
            new CompetitorPublicAttributes("price", convenience: 600, reputation: 600));

    internal static CompetitorOfferState StandardCompetitorOffer(
        string offerId,
        string? productId = "rice",
        long unitPriceCents = 450,
        int availableQuantity = 5,
        bool isActive = true) =>
        new(
            offerId,
            "grocery",
            productId,
            unitPriceCents,
            availableQuantity,
            isActive);

    internal static CompetitorMarketState StandardCompetitorState(
        string competitorId,
        bool isOpen = true,
        int availableServiceCapacity = 2,
        IEnumerable<CompetitorOfferState>? offers = null) =>
        new(
            competitorId,
            isOpen,
            availableServiceCapacity,
            offers ?? [StandardCompetitorOffer("rival-rice")]);

    internal static CompetitorOfferObservation StandardObservation(
        MarketPurchaseOpportunity opportunity,
        string competitorId,
        string offerId,
        string? observationId = null,
        int priceAttractiveness = 600) =>
        new(
            observationId ?? $"a-purchase-at-{competitorId}",
            opportunity.OpportunityId,
            competitorId,
            offerId,
            baseUtility: 0,
            StandardFeatures(priceAttractiveness));

    internal static ChoiceFeatures StandardFeatures(int priceAttractiveness = 600) => new(
        PriceAttractiveness: priceAttractiveness,
        QueueComfort: 600,
        Convenience: 600,
        Reputation: 600,
        Familiarity: 500);

    internal static CustomerChoiceSession RegularCustomer(string customerId) => new(
        customerId,
        new CustomerPreferenceProfile("regular", 200, 150, 150, 200, 300),
        new SoftmaxCustomerChoiceEngine(temperature: 100),
        learningRate: 100);

    internal static CustomerChoiceSession PriceSensitiveCustomer(string customerId) => new(
        customerId,
        new CustomerPreferenceProfile("price-sensitive", 700, 50, 50, 100, 100),
        new SoftmaxCustomerChoiceEngine(temperature: 100),
        learningRate: 100);

    internal static CustomerChoiceSession CustomerSelecting(
        string customerId,
        string optionId) => new(
        customerId,
        new CustomerPreferenceProfile("controlled", 200, 150, 150, 200, 300),
        new SelectOptionChoiceEngine(optionId),
        learningRate: 0);

    internal static StoreState NewOpenStore() => NewStore(isOpen: true);

    internal static StoreState NewStore(bool isOpen) => new(
        "boxbox",
        isOpen,
        new Dictionary<string, int> { ["rice"] = 5 },
        new Dictionary<string, long> { ["rice"] = 500 },
        cashBalanceCents: 1_000);

    internal static double ProbabilityFor(CustomerPurchaseFlowResult result, string shopId) =>
        result.Choice.Evaluations.Single(evaluation => evaluation.Option.ShopId == shopId).Probability;
}
