namespace Boxbox.Domain.Market;

public enum CompetitorSaleRejectionReason
{
    DuplicateDecision,
    WrongCompetitor,
    ShopClosed,
    NoServiceCapacity,
    UnknownOffer,
    OfferInactive,
    OfferDoesNotMatchOpportunity,
    InsufficientAvailability
}
