using System.Collections.ObjectModel;

namespace Boxbox.Domain.Market;

/// <summary>
/// Reduced current state used to decide whether a competitor can participate
/// in a market opportunity. It intentionally omits accounting and strategy.
/// </summary>
public sealed class CompetitorMarketState
{
    private readonly IReadOnlyList<CompetitorOfferState> _offers;
    private readonly HashSet<string> _completedDecisionIds;

    public CompetitorMarketState(
        string competitorId,
        bool isOpen,
        int availableServiceCapacity,
        IEnumerable<CompetitorOfferState> offers)
        : this(
            competitorId,
            isOpen,
            availableServiceCapacity,
            offers,
            Array.Empty<string>())
    {
    }

    private CompetitorMarketState(
        string competitorId,
        bool isOpen,
        int availableServiceCapacity,
        IEnumerable<CompetitorOfferState> offers,
        IEnumerable<string> completedDecisionIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competitorId);
        ArgumentNullException.ThrowIfNull(offers);
        if (availableServiceCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableServiceCapacity),
                "Available service capacity cannot be negative.");
        }

        var copiedOffers = new List<CompetitorOfferState>();
        var offerIds = new HashSet<string>(StringComparer.Ordinal);
        var targets = new HashSet<(string CategoryId, string? ProductId)>();
        foreach (var offer in offers)
        {
            ArgumentNullException.ThrowIfNull(offer);
            if (!offerIds.Add(offer.OfferId))
            {
                throw new ArgumentException(
                    $"Competitor '{competitorId}' repeats offer id '{offer.OfferId}'.",
                    nameof(offers));
            }

            if (!targets.Add((offer.CategoryId, offer.ProductId)))
            {
                var target = offer.ProductId ?? offer.CategoryId;
                throw new ArgumentException(
                    $"Competitor '{competitorId}' has more than one offer for target '{target}'.",
                    nameof(offers));
            }

            copiedOffers.Add(offer);
        }

        copiedOffers.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.OfferId, right.OfferId));

        CompetitorId = competitorId;
        IsOpen = isOpen;
        AvailableServiceCapacity = availableServiceCapacity;
        _offers = new ReadOnlyCollection<CompetitorOfferState>(copiedOffers);
        _completedDecisionIds = new HashSet<string>(completedDecisionIds, StringComparer.Ordinal);
    }

    public string CompetitorId { get; }
    public bool IsOpen { get; }
    public int AvailableServiceCapacity { get; }
    public IReadOnlyList<CompetitorOfferState> Offers => _offers;
    public IReadOnlyCollection<string> CompletedDecisionIds =>
        Array.AsReadOnly(_completedDecisionIds.Order(StringComparer.Ordinal).ToArray());

    public static CompetitorMarketState Restore(
        string competitorId,
        bool isOpen,
        int availableServiceCapacity,
        IEnumerable<CompetitorOfferState> offers,
        IEnumerable<string> completedDecisionIds) => new(
            competitorId,
            isOpen,
            availableServiceCapacity,
            offers,
            completedDecisionIds);

    /// <summary>
    /// Completes one summarized customer service in the current capacity
    /// window. One resolved opportunity consumes one capacity unit regardless
    /// of the number of product units in that purchase.
    /// </summary>
    public CompetitorSaleAttemptResult CompleteSale(CompetitorSaleCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedDecisionIds.Contains(command.DecisionId))
        {
            return CompetitorSaleAttemptResult.Rejected(
                this,
                CompetitorSaleRejectionReason.DuplicateDecision);
        }

        if (!string.Equals(command.CompetitorId, CompetitorId, StringComparison.Ordinal))
        {
            return CompetitorSaleAttemptResult.Rejected(
                this,
                CompetitorSaleRejectionReason.WrongCompetitor);
        }

        if (!IsOpen)
        {
            return CompetitorSaleAttemptResult.Rejected(
                this,
                CompetitorSaleRejectionReason.ShopClosed);
        }

        if (AvailableServiceCapacity <= 0)
        {
            return CompetitorSaleAttemptResult.Rejected(
                this,
                CompetitorSaleRejectionReason.NoServiceCapacity);
        }

        var offerIndex = _offers
            .Select((offer, index) => (offer, index))
            .FirstOrDefault(pair => string.Equals(
                pair.offer.OfferId,
                command.OfferId,
                StringComparison.Ordinal));
        if (offerIndex.offer is null)
        {
            return CompetitorSaleAttemptResult.Rejected(
                this,
                CompetitorSaleRejectionReason.UnknownOffer);
        }

        var offer = offerIndex.offer;
        if (!offer.IsActive)
        {
            return CompetitorSaleAttemptResult.Rejected(
                this,
                CompetitorSaleRejectionReason.OfferInactive);
        }

        if (!string.Equals(offer.CategoryId, command.CategoryId, StringComparison.Ordinal) ||
            (offer.ProductId is not null && !string.Equals(
                offer.ProductId,
                command.ProductId,
                StringComparison.Ordinal)))
        {
            return CompetitorSaleAttemptResult.Rejected(
                this,
                CompetitorSaleRejectionReason.OfferDoesNotMatchOpportunity);
        }

        if (offer.AvailableQuantity < command.Quantity)
        {
            return CompetitorSaleAttemptResult.Rejected(
                this,
                CompetitorSaleRejectionReason.InsufficientAvailability);
        }

        var totalCents = checked(offer.UnitPriceCents * command.Quantity);
        var nextOffers = _offers.ToArray();
        nextOffers[offerIndex.index] = offer.WithAvailableQuantity(
            offer.AvailableQuantity - command.Quantity);
        var nextCompletedDecisionIds = new HashSet<string>(
            _completedDecisionIds,
            StringComparer.Ordinal)
        {
            command.DecisionId
        };
        var nextState = new CompetitorMarketState(
            CompetitorId,
            IsOpen,
            AvailableServiceCapacity - 1,
            nextOffers,
            nextCompletedDecisionIds);
        var sale = new CompletedCompetitorSale(
            command.DecisionId,
            command.CustomerId,
            CompetitorId,
            offer.OfferId,
            command.ProductId,
            command.Quantity,
            offer.UnitPriceCents,
            totalCents);

        return CompetitorSaleAttemptResult.Succeeded(nextState, sale);
    }
}
