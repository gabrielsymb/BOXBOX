using System.Collections.ObjectModel;
using Boxbox.Domain.Catalog;
using Boxbox.Domain.Commerce;

namespace Boxbox.Domain.Market;

/// <summary>
/// Authoritative immutable economic snapshot for the shared market. A commit
/// replaces the player store, the selected competitor and the opportunity
/// record together, or returns this exact snapshot unchanged.
/// </summary>
public sealed class SharedMarketState
{
    private readonly IReadOnlyDictionary<string, CompetitorMarketState> _competitors;
    private readonly IReadOnlyDictionary<string, MarketOpportunityRecord> _opportunities;

    public SharedMarketState(
        ProductCatalog productCatalog,
        StoreState playerStore,
        IEnumerable<CompetitorMarketState> competitors,
        IEnumerable<MarketPurchaseOpportunity> opportunities)
        : this(
            productCatalog,
            playerStore,
            CopyCompetitors(playerStore, competitors),
            CreatePendingRecords(productCatalog, opportunities))
    {
    }

    private SharedMarketState(
        ProductCatalog productCatalog,
        StoreState playerStore,
        IReadOnlyDictionary<string, CompetitorMarketState> competitors,
        IReadOnlyDictionary<string, MarketOpportunityRecord> opportunities)
    {
        ArgumentNullException.ThrowIfNull(productCatalog);
        ArgumentNullException.ThrowIfNull(playerStore);
        ArgumentNullException.ThrowIfNull(competitors);
        ArgumentNullException.ThrowIfNull(opportunities);

        ProductCatalog = productCatalog;
        PlayerStore = playerStore;
        _competitors = new ReadOnlyDictionary<string, CompetitorMarketState>(
            new Dictionary<string, CompetitorMarketState>(competitors, StringComparer.Ordinal));
        _opportunities = new ReadOnlyDictionary<string, MarketOpportunityRecord>(
            new Dictionary<string, MarketOpportunityRecord>(opportunities, StringComparer.Ordinal));
    }

    public ProductCatalog ProductCatalog { get; }
    public StoreState PlayerStore { get; }
    public IReadOnlyDictionary<string, CompetitorMarketState> Competitors => _competitors;
    public IReadOnlyDictionary<string, MarketOpportunityRecord> Opportunities => _opportunities;

    public SharedMarketState ClosePlayerStore()
    {
        var closedStore = PlayerStore.Close();
        return ReferenceEquals(closedStore, PlayerStore)
            ? this
            : new SharedMarketState(
                ProductCatalog,
                closedStore,
                _competitors,
                _opportunities);
    }

    public MarketResolutionAttempt Resolve(ResolveMarketPurchaseCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_opportunities.TryGetValue(command.OpportunityId, out var record))
        {
            return MarketResolutionAttempt.Rejected(
                this,
                MarketResolutionRejectionReason.UnknownOpportunity);
        }

        if (record.Status == MarketOpportunityStatus.Resolved)
        {
            return MarketResolutionAttempt.Rejected(
                this,
                MarketResolutionRejectionReason.OpportunityAlreadyResolved);
        }

        if (_opportunities.Values.Any(existing => string.Equals(
                existing.Resolution?.DecisionId,
                command.DecisionId,
                StringComparison.Ordinal)))
        {
            return MarketResolutionAttempt.Rejected(
                this,
                MarketResolutionRejectionReason.DuplicateDecision);
        }

        var opportunity = record.Opportunity;
        if (opportunity.ProductId is null)
        {
            return MarketResolutionAttempt.Rejected(
                this,
                MarketResolutionRejectionReason.ProductRequired);
        }

        var nextPlayerStore = PlayerStore;
        var nextCompetitors = _competitors;
        CompletedSale? playerSale = null;
        CompletedCompetitorSale? competitorSale = null;

        switch (command.Outcome)
        {
            case MarketPurchaseOutcomeKind.PlayerStoreSale:
                {
                    var saleAttempt = PlayerStore.CompleteSale(new SaleCommand(
                        command.DecisionId,
                        command.CustomerId,
                        command.SelectedShopId!,
                        opportunity.ProductId,
                        opportunity.Quantity));
                    if (!saleAttempt.Completed)
                    {
                        return MarketResolutionAttempt.Rejected(
                            this,
                            MarketResolutionRejectionReason.PlayerSaleRejected,
                            playerSaleReason: saleAttempt.RejectionReason);
                    }

                    nextPlayerStore = saleAttempt.ResultingState;
                    playerSale = saleAttempt.Sale;
                    break;
                }

            case MarketPurchaseOutcomeKind.CompetitorSale:
                {
                    if (!_competitors.TryGetValue(command.SelectedShopId!, out var competitor))
                    {
                        return MarketResolutionAttempt.Rejected(
                            this,
                            MarketResolutionRejectionReason.UnknownCompetitor);
                    }

                    var saleAttempt = competitor.CompleteSale(new CompetitorSaleCommand(
                        command.DecisionId,
                        command.CustomerId,
                        command.SelectedShopId!,
                        command.SelectedOfferId!,
                        opportunity.CategoryId,
                        opportunity.ProductId,
                        opportunity.Quantity));
                    if (!saleAttempt.Completed)
                    {
                        return MarketResolutionAttempt.Rejected(
                            this,
                            MarketResolutionRejectionReason.CompetitorSaleRejected,
                            competitorSaleReason: saleAttempt.RejectionReason);
                    }

                    var copiedCompetitors = new Dictionary<string, CompetitorMarketState>(
                        _competitors,
                        StringComparer.Ordinal)
                    {
                        [competitor.CompetitorId] = saleAttempt.ResultingState
                    };
                    nextCompetitors = new ReadOnlyDictionary<string, CompetitorMarketState>(copiedCompetitors);
                    competitorSale = saleAttempt.Sale;
                    break;
                }

            case MarketPurchaseOutcomeKind.ChoseNotToPurchase:
            case MarketPurchaseOutcomeKind.DemandUnserved:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }

        var unitPriceCents = playerSale?.UnitPriceCents ?? competitorSale?.UnitPriceCents;
        var totalCents = playerSale?.TotalCents ?? competitorSale?.TotalCents;
        var resolution = new MarketOpportunityResolved(
            opportunity.OpportunityId,
            command.DecisionId,
            command.CustomerId,
            command.Outcome,
            command.SelectedShopId,
            command.SelectedOfferId,
            opportunity.ProductId,
            opportunity.Quantity,
            unitPriceCents,
            totalCents);
        var copiedOpportunities = new Dictionary<string, MarketOpportunityRecord>(
            _opportunities,
            StringComparer.Ordinal)
        {
            [opportunity.OpportunityId] = record.Resolve(resolution)
        };
        var nextState = new SharedMarketState(
            ProductCatalog,
            nextPlayerStore,
            nextCompetitors,
            copiedOpportunities);
        SharedMarketInvariantRegistry.VerifyCommit(
            this,
            nextState,
            resolution,
            playerSale,
            competitorSale);

        return MarketResolutionAttempt.Succeeded(
            nextState,
            resolution,
            playerSale,
            competitorSale);
    }

    private static IReadOnlyDictionary<string, CompetitorMarketState> CopyCompetitors(
        StoreState playerStore,
        IEnumerable<CompetitorMarketState> competitors)
    {
        ArgumentNullException.ThrowIfNull(playerStore);
        ArgumentNullException.ThrowIfNull(competitors);

        var result = new Dictionary<string, CompetitorMarketState>(StringComparer.Ordinal);
        foreach (var competitor in competitors)
        {
            ArgumentNullException.ThrowIfNull(competitor);
            if (string.Equals(competitor.CompetitorId, playerStore.ShopId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A competitor cannot reuse the player store id.",
                    nameof(competitors));
            }

            if (!result.TryAdd(competitor.CompetitorId, competitor))
            {
                throw new ArgumentException(
                    $"Competitor id '{competitor.CompetitorId}' is duplicated.",
                    nameof(competitors));
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<string, MarketOpportunityRecord> CreatePendingRecords(
        ProductCatalog productCatalog,
        IEnumerable<MarketPurchaseOpportunity> opportunities)
    {
        ArgumentNullException.ThrowIfNull(productCatalog);
        ArgumentNullException.ThrowIfNull(opportunities);

        var result = new Dictionary<string, MarketOpportunityRecord>(StringComparer.Ordinal);
        foreach (var opportunity in opportunities)
        {
            ArgumentNullException.ThrowIfNull(opportunity);
            if (opportunity.ProductId is not null)
            {
                _ = productCatalog.RequireProductInCategory(
                    opportunity.ProductId,
                    opportunity.CategoryId);
            }

            if (!result.TryAdd(
                    opportunity.OpportunityId,
                    new MarketOpportunityRecord(opportunity)))
            {
                throw new ArgumentException(
                    $"Market opportunity id '{opportunity.OpportunityId}' is duplicated.",
                    nameof(opportunities));
            }
        }

        return result;
    }
}
