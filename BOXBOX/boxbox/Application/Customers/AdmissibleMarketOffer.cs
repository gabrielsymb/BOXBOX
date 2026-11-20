using Boxbox.AI.Contracts;

namespace Boxbox.Application.Customers;

/// <summary>
/// A shop offer that an upstream market passability step has already confirmed
/// as eligible for the current purchase opportunity. It contains only what the
/// customer is allowed to perceive.
/// </summary>
public sealed class AdmissibleMarketOffer
{
    internal AdmissibleMarketOffer(
        string optionId,
        string shopId,
        string offerId,
        int baseUtility,
        ChoiceFeatures perceivedFeatures)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(offerId);
        if (baseUtility is < -1000 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseUtility),
                "Offer base utility must be between -1000 and 1000.");
        }

        perceivedFeatures.Validate();
        OptionId = optionId;
        ShopId = shopId;
        OfferId = offerId;
        BaseUtility = baseUtility;
        PerceivedFeatures = perceivedFeatures;
    }

    public string OptionId { get; }
    public string ShopId { get; }
    internal string OfferId { get; }
    public int BaseUtility { get; }
    public ChoiceFeatures PerceivedFeatures { get; }

    internal ChoiceOption ToChoiceOption() => new(
        OptionId,
        ChoiceOptionKind.PurchaseAtShop,
        BaseUtility,
        ShopId,
        PerceivedFeatures);
}
