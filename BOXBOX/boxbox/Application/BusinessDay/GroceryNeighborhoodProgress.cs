namespace Boxbox.Application.BusinessDay;

public enum SupplierConversation
{
    None,
    Welcome,
    CoffeeOffer,
    RegularCoffeeOffer
}

/// <summary>Interaction and personal spending state for one Day 1 attempt.</summary>
public sealed class GroceryNeighborhoodProgress
{
    public const long OpeningWalletCents = 2_000;

    private ShopLocation _location = ShopLocation.Grocery;
    private readonly Stack<ShopLocation> _previousLocations = new();
    private bool _greeted;
    private bool _boughtCoffee;
    private long? _pendingCoffeePrice;

    public long WalletCents { get; private set; } = OpeningWalletCents;
    public bool HasConfirmedOrder { get; private set; }
    public bool DayStarted { get; private set; }
    public bool AtSupplier => _location == ShopLocation.Supplier;
    public bool CanTalk => AtSupplier && !_boughtCoffee && !_pendingCoffeePrice.HasValue && !CoffeeOfferCents.HasValue;
    public long? CoffeeOfferCents { get; private set; }

    public bool TryVisitSupplier(out SupplierConversation greeting)
    {
        greeting = SupplierConversation.None;
        if (AtSupplier || DayStarted)
        {
            return false;
        }

        _previousLocations.Push(_location);
        _location = ShopLocation.Supplier;
        if (!_greeted)
        {
            _greeted = true;
            greeting = SupplierConversation.Welcome;
        }
        return true;
    }

    public bool TryGoBack()
    {
        if (!_previousLocations.TryPop(out var previous))
        {
            return false;
        }

        _location = previous;
        _pendingCoffeePrice = null;
        CoffeeOfferCents = null;
        return true;
    }

    public void RecordConfirmedOrder() => HasConfirmedOrder = true;

    public bool TryStartDay()
    {
        if (AtSupplier || !HasConfirmedOrder || DayStarted)
        {
            return false;
        }

        DayStarted = true;
        return true;
    }

    public SupplierConversation BeginSupplierConversation()
    {
        if (!CanTalk)
        {
            return SupplierConversation.None;
        }

        _pendingCoffeePrice = HasConfirmedOrder ? 400 : 600;
        return HasConfirmedOrder ? SupplierConversation.RegularCoffeeOffer : SupplierConversation.CoffeeOffer;
    }

    public void CompleteSupplierConversation()
    {
        if (AtSupplier && _pendingCoffeePrice is { } price)
        {
            CoffeeOfferCents = price;
            _pendingCoffeePrice = null;
        }
    }

    public bool TryBuyCoffee()
    {
        if (!AtSupplier || CoffeeOfferCents is not { } price || _boughtCoffee || WalletCents < price)
        {
            return false;
        }

        WalletCents -= price;
        _boughtCoffee = true;
        CoffeeOfferCents = null;
        return true;
    }

    private enum ShopLocation
    {
        Grocery,
        Supplier
    }
}
