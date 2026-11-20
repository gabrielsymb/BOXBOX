using Boxbox.Domain.Commerce;

namespace Boxbox.Domain.Market;

/// <summary>
/// Mandatory postcondition registry for shared-market commits. No policy,
/// content rule or behavior engine can choose which of these checks runs.
/// </summary>
internal static class SharedMarketInvariantRegistry
{
    internal static void VerifyCommit(
        SharedMarketState previous,
        SharedMarketState proposed,
        MarketOpportunityResolved resolution,
        CompletedSale? playerSale,
        CompletedCompetitorSale? competitorSale)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(proposed);
        ArgumentNullException.ThrowIfNull(resolution);

        if (!previous.Opportunities.TryGetValue(resolution.OpportunityId, out var previousRecord) ||
            previousRecord.Status != MarketOpportunityStatus.Pending)
        {
            throw new InvalidOperationException(
                "A market commit must start from one pending opportunity.");
        }

        if (!proposed.Opportunities.TryGetValue(resolution.OpportunityId, out var proposedRecord) ||
            proposedRecord.Status != MarketOpportunityStatus.Resolved ||
            !ReferenceEquals(proposedRecord.Resolution, resolution))
        {
            throw new InvalidOperationException(
                "A market commit must resolve exactly its authoritative opportunity record.");
        }

        if (previous.Opportunities.Count != proposed.Opportunities.Count ||
            previous.Competitors.Count != proposed.Competitors.Count)
        {
            throw new InvalidOperationException(
                "A market resolution cannot create or remove opportunities or competitors.");
        }

        if (!ReferenceEquals(previous.ProductCatalog, proposed.ProductCatalog) ||
            previous.Opportunities.Any(pair =>
                !string.Equals(pair.Key, resolution.OpportunityId, StringComparison.Ordinal) &&
                (!proposed.Opportunities.TryGetValue(pair.Key, out var nextRecord) ||
                 !ReferenceEquals(pair.Value, nextRecord))))
        {
            throw new InvalidOperationException(
                "A market resolution may replace only its own opportunity record.");
        }

        if (!string.Equals(
                previousRecord.Opportunity.ProductId,
                resolution.ProductId,
                StringComparison.Ordinal) ||
            previousRecord.Opportunity.Quantity != resolution.Quantity)
        {
            throw new InvalidOperationException(
                "A market resolution must preserve the authoritative product and quantity.");
        }

        switch (resolution.Outcome)
        {
            case MarketPurchaseOutcomeKind.PlayerStoreSale:
                VerifyPlayerSale(previous, proposed, resolution, playerSale, competitorSale);
                break;

            case MarketPurchaseOutcomeKind.CompetitorSale:
                VerifyCompetitorSale(previous, proposed, resolution, playerSale, competitorSale);
                break;

            case MarketPurchaseOutcomeKind.ChoseNotToPurchase:
            case MarketPurchaseOutcomeKind.DemandUnserved:
                VerifyNoSale(previous, proposed, resolution, playerSale, competitorSale);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(resolution));
        }
    }

    private static void VerifyPlayerSale(
        SharedMarketState previous,
        SharedMarketState proposed,
        MarketOpportunityResolved resolution,
        CompletedSale? playerSale,
        CompletedCompetitorSale? competitorSale)
    {
        if (playerSale is null || competitorSale is not null ||
            resolution.OfferId is not null ||
            !string.Equals(resolution.ShopId, previous.PlayerStore.ShopId, StringComparison.Ordinal) ||
            !string.Equals(playerSale.DecisionId, resolution.DecisionId, StringComparison.Ordinal) ||
            !string.Equals(playerSale.CustomerId, resolution.CustomerId, StringComparison.Ordinal) ||
            !string.Equals(playerSale.ShopId, resolution.ShopId, StringComparison.Ordinal) ||
            !string.Equals(playerSale.ProductId, resolution.ProductId, StringComparison.Ordinal) ||
            playerSale.Quantity != resolution.Quantity ||
            playerSale.UnitPriceCents != resolution.UnitPriceCents ||
            playerSale.TotalCents != resolution.TotalCents)
        {
            throw new InvalidOperationException(
                "A player-store resolution must match exactly one completed player sale.");
        }

        if (ReferenceEquals(previous.PlayerStore, proposed.PlayerStore) ||
            !SameCompetitorReferences(previous, proposed))
        {
            throw new InvalidOperationException(
                "A player-store sale may change only the player store and opportunity record.");
        }
    }

    private static void VerifyCompetitorSale(
        SharedMarketState previous,
        SharedMarketState proposed,
        MarketOpportunityResolved resolution,
        CompletedSale? playerSale,
        CompletedCompetitorSale? competitorSale)
    {
        if (playerSale is not null || competitorSale is null ||
            resolution.ShopId is null || resolution.OfferId is null ||
            !string.Equals(competitorSale.CompetitorId, resolution.ShopId, StringComparison.Ordinal) ||
            !string.Equals(competitorSale.OfferId, resolution.OfferId, StringComparison.Ordinal) ||
            !string.Equals(competitorSale.DecisionId, resolution.DecisionId, StringComparison.Ordinal) ||
            !string.Equals(competitorSale.CustomerId, resolution.CustomerId, StringComparison.Ordinal) ||
            !string.Equals(competitorSale.ProductId, resolution.ProductId, StringComparison.Ordinal) ||
            competitorSale.Quantity != resolution.Quantity ||
            competitorSale.UnitPriceCents != resolution.UnitPriceCents ||
            competitorSale.TotalCents != resolution.TotalCents)
        {
            throw new InvalidOperationException(
                "A competitor resolution must match exactly one completed competitor sale.");
        }

        if (!ReferenceEquals(previous.PlayerStore, proposed.PlayerStore))
        {
            throw new InvalidOperationException(
                "A competitor sale cannot change the player store.");
        }

        foreach (var (competitorId, previousCompetitor) in previous.Competitors)
        {
            var proposedCompetitor = proposed.Competitors[competitorId];
            var isSelected = string.Equals(
                competitorId,
                competitorSale.CompetitorId,
                StringComparison.Ordinal);
            if (isSelected == ReferenceEquals(previousCompetitor, proposedCompetitor))
            {
                throw new InvalidOperationException(
                    "A competitor sale must change only the selected competitor.");
            }
        }
    }

    private static void VerifyNoSale(
        SharedMarketState previous,
        SharedMarketState proposed,
        MarketOpportunityResolved resolution,
        CompletedSale? playerSale,
        CompletedCompetitorSale? competitorSale)
    {
        if (playerSale is not null || competitorSale is not null ||
            resolution.ShopId is not null || resolution.OfferId is not null ||
            resolution.UnitPriceCents is not null || resolution.TotalCents is not null ||
            !ReferenceEquals(previous.PlayerStore, proposed.PlayerStore) ||
            !SameCompetitorReferences(previous, proposed))
        {
            throw new InvalidOperationException(
                "A no-sale outcome cannot change economic assets.");
        }
    }

    private static bool SameCompetitorReferences(
        SharedMarketState previous,
        SharedMarketState proposed) =>
        previous.Competitors.All(pair =>
            proposed.Competitors.TryGetValue(pair.Key, out var competitor) &&
            ReferenceEquals(pair.Value, competitor));
}
