namespace Boxbox.AI.Contracts;

public enum ChoiceOptionKind
{
    PurchaseAtShop,
    DoNotPurchase
}

public sealed class ChoiceOption
{
    public ChoiceOption(string id, ChoiceOptionKind kind, int baseUtility, string? shopId, ChoiceFeatures features)
    {
        if (kind is not ChoiceOptionKind.PurchaseAtShop and not ChoiceOptionKind.DoNotPurchase)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), "Unknown choice option kind.");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("An option must have a stable id.", nameof(id));
        }

        if (baseUtility is < -1000 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(baseUtility), "Base utility must be between -1000 and 1000.");
        }

        if (kind == ChoiceOptionKind.PurchaseAtShop && string.IsNullOrWhiteSpace(shopId))
        {
            throw new ArgumentException("A purchase option must identify its shop.", nameof(shopId));
        }

        if (kind == ChoiceOptionKind.DoNotPurchase && !string.IsNullOrEmpty(shopId))
        {
            throw new ArgumentException("A non-purchase option cannot identify a shop.", nameof(shopId));
        }

        if (kind == ChoiceOptionKind.DoNotPurchase && features != default)
        {
            throw new ArgumentException("A non-purchase option cannot have shop attributes.", nameof(features));
        }

        features.Validate();
        Id = id;
        Kind = kind;
        BaseUtility = baseUtility;
        ShopId = shopId;
        Features = features;
    }

    public string Id { get; }
    public ChoiceOptionKind Kind { get; }
    public int BaseUtility { get; }
    public string? ShopId { get; }
    public ChoiceFeatures Features { get; }
}
