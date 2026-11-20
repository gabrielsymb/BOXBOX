namespace Boxbox.Application.Market;

/// <summary>
/// Stable technical reason codes for replay, tests and diagnostics. These may
/// reveal real state and are not player-facing localization keys.
/// </summary>
internal enum MarketOfferPassabilityReason
{
    MissingCurrentState,
    CategoryNotServed,
    ShopClosed,
    NoServiceCapacity,
    NoMatchingOffer,
    OfferInactive,
    InsufficientAvailability,
    MissingPermittedObservation
}
