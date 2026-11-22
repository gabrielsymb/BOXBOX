using Boxbox.AI.Contracts;
using Boxbox.AI.Customers;
using Boxbox.Application.Customers;
using Boxbox.Domain.Market;

namespace Boxbox.Application.Market;

/// <summary>
/// Application boundary for one shared-market purchase opportunity. Behavior
/// chooses only an intention; SharedMarketState validates and commits the full
/// economic outcome and opportunity record atomically.
/// </summary>
public sealed class SharedMarketPurchaseService
{
    private readonly AdmissibleMarketOfferGenerator _offerGenerator;
    private readonly CustomerPurchaseService _purchaseService;

    public SharedMarketPurchaseService(
        AdmissibleMarketOfferGenerator offerGenerator,
        CustomerPurchaseService purchaseService)
    {
        ArgumentNullException.ThrowIfNull(offerGenerator);
        ArgumentNullException.ThrowIfNull(purchaseService);

        _offerGenerator = offerGenerator;
        _purchaseService = purchaseService;
    }

    public SharedMarketPurchaseResult Execute(
        CustomerChoiceSession customer,
        SharedMarketState marketState,
        string opportunityId,
        string decisionId,
        ChoiceFeatures playerStorePerceivedFeatures,
        IReadOnlyCollection<CompetitorProfile> competitorProfiles,
        IReadOnlyCollection<CompetitorOfferObservation> permittedObservations,
        IChoiceRandomSource randomSource)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(marketState);
        ArgumentException.ThrowIfNullOrWhiteSpace(opportunityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentNullException.ThrowIfNull(competitorProfiles);
        ArgumentNullException.ThrowIfNull(permittedObservations);
        ArgumentNullException.ThrowIfNull(randomSource);
        playerStorePerceivedFeatures.Validate();

        if (!marketState.Opportunities.TryGetValue(opportunityId, out var opportunityRecord))
        {
            return RejectedBeforeChoice(
                marketState,
                MarketResolutionRejectionReason.UnknownOpportunity);
        }

        if (opportunityRecord.Status == MarketOpportunityStatus.Resolved)
        {
            return RejectedBeforeChoice(
                marketState,
                MarketResolutionRejectionReason.OpportunityAlreadyResolved);
        }

        if (marketState.Opportunities.Values.Any(existing => string.Equals(
                existing.Resolution?.DecisionId,
                decisionId,
                StringComparison.Ordinal)))
        {
            return RejectedBeforeChoice(
                marketState,
                MarketResolutionRejectionReason.DuplicateDecision);
        }

        var opportunity = opportunityRecord.Opportunity;
        if (opportunity.ProductId is null)
        {
            return RejectedBeforeChoice(
                marketState,
                MarketResolutionRejectionReason.ProductRequired);
        }

        var generation = _offerGenerator.Generate(
            opportunity,
            competitorProfiles,
            marketState.Competitors.Values.ToArray(),
            permittedObservations);
        var intent = _purchaseService.Decide(
            customer,
            marketState.PlayerStore,
            decisionId,
            opportunity.ProductId,
            opportunity.Quantity,
            playerStorePerceivedFeatures,
            generation.AdmissibleOffers,
            randomSource,
            opportunity.SpendingLimitCents);

        var command = CreateResolutionCommand(
            customer.CustomerId,
            opportunity.OpportunityId,
            intent,
            marketState.PlayerStore.ShopId,
            generation.AdmissibleOffers);
        var resolutionAttempt = marketState.Resolve(command);
        var purchaseFlow = BuildPurchaseFlow(
            intent,
            marketState,
            resolutionAttempt);

        return new SharedMarketPurchaseResult(
            resolutionAttempt.Completed
                ? SharedMarketPurchaseStatus.Resolved
                : SharedMarketPurchaseStatus.Rejected,
            resolutionAttempt,
            generation,
            purchaseFlow);
    }

    private static ResolveMarketPurchaseCommand CreateResolutionCommand(
        string customerId,
        string opportunityId,
        CustomerPurchaseIntent intent,
        string playerStoreId,
        IReadOnlyCollection<AdmissibleMarketOffer> competitorOffers)
    {
        if (intent.Kind == CustomerPurchaseIntentKind.DemandUnserved)
        {
            return new ResolveMarketPurchaseCommand(
                opportunityId,
                intent.Choice.DecisionId,
                customerId,
                MarketPurchaseOutcomeKind.DemandUnserved);
        }

        if (intent.Kind == CustomerPurchaseIntentKind.ChoseNotToPurchase)
        {
            return new ResolveMarketPurchaseCommand(
                opportunityId,
                intent.Choice.DecisionId,
                customerId,
                MarketPurchaseOutcomeKind.ChoseNotToPurchase);
        }

        var selected = intent.Choice.Selected.Option;
        if (string.Equals(selected.ShopId, playerStoreId, StringComparison.Ordinal))
        {
            return new ResolveMarketPurchaseCommand(
                opportunityId,
                intent.Choice.DecisionId,
                customerId,
                MarketPurchaseOutcomeKind.PlayerStoreSale,
                selected.ShopId);
        }

        var selectedCompetitorOffer = competitorOffers.Single(offer =>
            string.Equals(offer.OptionId, selected.Id, StringComparison.Ordinal) &&
            string.Equals(offer.ShopId, selected.ShopId, StringComparison.Ordinal));
        return new ResolveMarketPurchaseCommand(
            opportunityId,
            intent.Choice.DecisionId,
            customerId,
            MarketPurchaseOutcomeKind.CompetitorSale,
            selected.ShopId,
            selectedCompetitorOffer.OfferId);
    }

    private static CustomerPurchaseFlowResult BuildPurchaseFlow(
        CustomerPurchaseIntent intent,
        SharedMarketState originalState,
        MarketResolutionAttempt resolutionAttempt)
    {
        if (!resolutionAttempt.Completed)
        {
            return new CustomerPurchaseFlowResult(
                CustomerPurchaseFlowStatus.SaleRejected,
                intent.Choice,
                originalState.PlayerStore,
                null,
                resolutionAttempt.PlayerSaleRejectionReason);
        }

        var status = resolutionAttempt.Resolution!.Outcome switch
        {
            MarketPurchaseOutcomeKind.PlayerStoreSale => CustomerPurchaseFlowStatus.SaleCompleted,
            MarketPurchaseOutcomeKind.CompetitorSale => CustomerPurchaseFlowStatus.ChoseAlternativeStore,
            MarketPurchaseOutcomeKind.ChoseNotToPurchase => CustomerPurchaseFlowStatus.ChoseNotToPurchase,
            MarketPurchaseOutcomeKind.DemandUnserved => CustomerPurchaseFlowStatus.DemandUnserved,
            _ => throw new ArgumentOutOfRangeException(nameof(resolutionAttempt))
        };

        return new CustomerPurchaseFlowResult(
            status,
            intent.Choice,
            resolutionAttempt.ResultingState.PlayerStore,
            resolutionAttempt.PlayerSale,
            null);
    }

    private static SharedMarketPurchaseResult RejectedBeforeChoice(
        SharedMarketState state,
        MarketResolutionRejectionReason reason) =>
        new(
            SharedMarketPurchaseStatus.Rejected,
            MarketResolutionAttempt.Rejected(state, reason),
            null,
            null);
}
