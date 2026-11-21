using Boxbox.AI.Contracts;
using Boxbox.AI.Customers;
using Boxbox.Domain.Commerce;

namespace Boxbox.Application.Customers;

public sealed class CustomerPurchaseService
{
    private readonly int _noPurchaseBaseUtility;

    public CustomerPurchaseService(int noPurchaseBaseUtility)
    {
        if (noPurchaseBaseUtility is < -1000 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(noPurchaseBaseUtility),
                "No-purchase utility must be between -1000 and 1000.");
        }

        _noPurchaseBaseUtility = noPurchaseBaseUtility;
    }

    /// <summary>
    /// Laboratory-only path with no competitors. Gameplay market demand must go
    /// through SharedMarketPurchaseService so it cannot bypass shared demand.
    /// </summary>
    public CustomerPurchaseFlowResult ExecutePlayerStoreOnly(
        CustomerChoiceSession customer,
        StoreState store,
        string decisionId,
        string productId,
        int quantity,
        ChoiceFeatures perceivedFeatures,
        IChoiceRandomSource randomSource)
    {
        return Execute(
            customer,
            store,
            decisionId,
            productId,
            quantity,
            perceivedFeatures,
            [],
            randomSource);
    }

    internal CustomerPurchaseFlowResult Execute(
        CustomerChoiceSession customer,
        StoreState store,
        string decisionId,
        string productId,
        int quantity,
        ChoiceFeatures playerStorePerceivedFeatures,
        IReadOnlyCollection<AdmissibleMarketOffer> alternativeOffers,
        IChoiceRandomSource randomSource)
    {
        var intent = Decide(
            customer,
            store,
            decisionId,
            productId,
            quantity,
            playerStorePerceivedFeatures,
            alternativeOffers,
            randomSource);

        if (intent.Kind != CustomerPurchaseIntentKind.ChoseStore)
        {
            return new CustomerPurchaseFlowResult(
                intent.Kind == CustomerPurchaseIntentKind.DemandUnserved
                    ? CustomerPurchaseFlowStatus.DemandUnserved
                    : CustomerPurchaseFlowStatus.ChoseNotToPurchase,
                intent.Choice,
                store,
                null,
                null);
        }

        if (!string.Equals(intent.Choice.Selected.Option.ShopId, store.ShopId, StringComparison.Ordinal))
        {
            return new CustomerPurchaseFlowResult(
                CustomerPurchaseFlowStatus.ChoseAlternativeStore,
                intent.Choice,
                store,
                null,
                null);
        }

        var command = new SaleCommand(
            intent.Choice.DecisionId,
            customer.CustomerId,
            intent.Choice.Selected.Option.ShopId!,
            productId,
            quantity);
        var saleAttempt = store.CompleteSale(command);
        return saleAttempt.Completed
            ? new CustomerPurchaseFlowResult(
                CustomerPurchaseFlowStatus.SaleCompleted,
                intent.Choice,
                saleAttempt.ResultingState,
                saleAttempt.Sale,
                null)
            : new CustomerPurchaseFlowResult(
                CustomerPurchaseFlowStatus.SaleRejected,
                intent.Choice,
                store,
                null,
                saleAttempt.RejectionReason);
    }

    internal CustomerPurchaseIntent Decide(
        CustomerChoiceSession customer,
        StoreState store,
        string decisionId,
        string productId,
        int quantity,
        ChoiceFeatures playerStorePerceivedFeatures,
        IReadOnlyCollection<AdmissibleMarketOffer> alternativeOffers,
        IChoiceRandomSource randomSource,
        long? spendingLimitCents = null)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentNullException.ThrowIfNull(alternativeOffers);
        ArgumentNullException.ThrowIfNull(randomSource);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Purchase quantity must be positive.");
        }

        const string noPurchaseOptionId = "do-not-purchase";
        var playerStoreOptionId = $"purchase-at-{store.ShopId}";
        var options = new List<ChoiceOption>(alternativeOffers.Count + 2);
        if (store.CanFulfill(productId, quantity) &&
            (spendingLimitCents is not { } limit ||
                (store.TryGetUnitPriceCents(productId, out var price) && price <= limit / quantity)))
        {
            options.Add(new ChoiceOption(
                playerStoreOptionId,
                ChoiceOptionKind.PurchaseAtShop,
                0,
                store.ShopId,
                playerStorePerceivedFeatures));
        }

        var seenShopIds = new HashSet<string>(StringComparer.Ordinal)
        {
            store.ShopId
        };
        var seenOptionIds = new HashSet<string>(StringComparer.Ordinal)
        {
            playerStoreOptionId,
            noPurchaseOptionId
        };
        foreach (var offer in alternativeOffers.OrderBy(
                     static offer => offer?.OptionId,
                     StringComparer.Ordinal))
        {
            ArgumentNullException.ThrowIfNull(offer);
            if (!seenShopIds.Add(offer.ShopId))
            {
                throw new ArgumentException(
                    $"Only one admissible offer per shop is allowed: {offer.ShopId}",
                    nameof(alternativeOffers));
            }

            if (!seenOptionIds.Add(offer.OptionId))
            {
                throw new ArgumentException(
                    $"Market option id is reserved or duplicated: {offer.OptionId}",
                    nameof(alternativeOffers));
            }

            options.Add(offer.ToChoiceOption());
        }

        options.Add(new ChoiceOption(
            noPurchaseOptionId,
            ChoiceOptionKind.DoNotPurchase,
            _noPurchaseBaseUtility,
            null,
            default));

        var choice = customer.Decide(decisionId, options, randomSource);
        if (choice.Selected.Option.Kind == ChoiceOptionKind.DoNotPurchase)
        {
            return new CustomerPurchaseIntent(
                options.Count == 1
                    ? CustomerPurchaseIntentKind.DemandUnserved
                    : CustomerPurchaseIntentKind.ChoseNotToPurchase,
                choice);
        }

        return new CustomerPurchaseIntent(CustomerPurchaseIntentKind.ChoseStore, choice);
    }
}
