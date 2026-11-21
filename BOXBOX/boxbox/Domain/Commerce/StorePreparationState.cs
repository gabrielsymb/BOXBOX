using System.Collections.ObjectModel;
using Boxbox.Domain.Catalog;

namespace Boxbox.Domain.Commerce;

public sealed record PurchaseInventoryCommand(
    string CommandId,
    string OfferId,
    int Quantity);

public sealed record SetProductPriceCommand(
    string CommandId,
    string ProductId,
    long UnitPriceCents);

public sealed record CompletedInventoryPurchase(
    string CommandId,
    string OfferId,
    string SupplierId,
    string ProductId,
    SupplierChannel Channel,
    int Quantity,
    long UnitCostCents,
    long TotalCostCents);

public enum PreparationRejectionReason
{
    DuplicateCommand,
    UnknownProduct,
    OfferMismatch,
    QuantityOutsideOffer,
    InsufficientCash,
    InvalidPrice,
    NoStock,
    MissingPrice
}

public sealed record PreparationAttempt<T>(
    bool Completed,
    StorePreparationState ResultingState,
    T? Completion,
    PreparationRejectionReason? RejectionReason)
    where T : class;

/// <summary>
/// Immutable pre-opening state. Purchases and pricing either replace the whole
/// state or return the exact unchanged instance.
/// </summary>
public sealed class StorePreparationState
{
    private readonly ProductCatalog _catalog;
    private readonly IReadOnlyDictionary<string, int> _stockByProduct;
    private readonly IReadOnlyDictionary<string, long> _unitPriceCents;
    private readonly IReadOnlyList<CompletedInventoryPurchase> _purchases;
    private readonly HashSet<string> _completedCommandIds;

    public StorePreparationState(ProductCatalog catalog, string shopId, long cashBalanceCents)
        : this(
            catalog,
            shopId,
            cashBalanceCents,
            new Dictionary<string, int>(StringComparer.Ordinal),
            new Dictionary<string, long>(StringComparer.Ordinal),
            Array.Empty<CompletedInventoryPurchase>(),
            Array.Empty<string>())
    {
    }

    private StorePreparationState(
        ProductCatalog catalog,
        string shopId,
        long cashBalanceCents,
        IReadOnlyDictionary<string, int> stockByProduct,
        IReadOnlyDictionary<string, long> unitPriceCents,
        IEnumerable<CompletedInventoryPurchase> purchases,
        IEnumerable<string> completedCommandIds)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        if (cashBalanceCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cashBalanceCents));
        }

        _catalog = catalog;
        ShopId = shopId;
        CashBalanceCents = cashBalanceCents;
        _stockByProduct = new ReadOnlyDictionary<string, int>(
            new Dictionary<string, int>(stockByProduct, StringComparer.Ordinal));
        _unitPriceCents = new ReadOnlyDictionary<string, long>(
            new Dictionary<string, long>(unitPriceCents, StringComparer.Ordinal));
        _purchases = Array.AsReadOnly(purchases.ToArray());
        _completedCommandIds = new HashSet<string>(completedCommandIds, StringComparer.Ordinal);
    }

    public string ShopId { get; }
    public long CashBalanceCents { get; }
    public IReadOnlyDictionary<string, int> StockByProduct => _stockByProduct;
    public IReadOnlyDictionary<string, long> UnitPriceCents => _unitPriceCents;
    public IReadOnlyList<CompletedInventoryPurchase> Purchases => _purchases;

    public PreparationAttempt<CompletedInventoryPurchase> Purchase(
        PurchaseInventoryCommand command,
        SupplierOffer offer)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CommandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.OfferId);

        if (_completedCommandIds.Contains(command.CommandId))
        {
            return Reject<CompletedInventoryPurchase>(PreparationRejectionReason.DuplicateCommand);
        }

        if (!_catalog.Products.Any(product => string.Equals(
                product.ProductId,
                offer.ProductId,
                StringComparison.Ordinal)))
        {
            return Reject<CompletedInventoryPurchase>(PreparationRejectionReason.UnknownProduct);
        }

        if (!string.Equals(command.OfferId, offer.OfferId, StringComparison.Ordinal))
        {
            return Reject<CompletedInventoryPurchase>(PreparationRejectionReason.OfferMismatch);
        }

        if (command.Quantity < offer.MinimumQuantity || command.Quantity > offer.MaximumQuantity)
        {
            return Reject<CompletedInventoryPurchase>(PreparationRejectionReason.QuantityOutsideOffer);
        }

        var totalCostCents = checked(offer.UnitCostCents * command.Quantity);
        if (totalCostCents > CashBalanceCents)
        {
            return Reject<CompletedInventoryPurchase>(PreparationRejectionReason.InsufficientCash);
        }

        var nextStock = new Dictionary<string, int>(_stockByProduct, StringComparer.Ordinal)
        {
            [offer.ProductId] = checked(
                _stockByProduct.GetValueOrDefault(offer.ProductId) + command.Quantity)
        };
        var completion = new CompletedInventoryPurchase(
            command.CommandId,
            offer.OfferId,
            offer.SupplierId,
            offer.ProductId,
            offer.Channel,
            command.Quantity,
            offer.UnitCostCents,
            totalCostCents);
        var nextPurchases = _purchases.Append(completion);
        var nextCommands = new HashSet<string>(_completedCommandIds, StringComparer.Ordinal)
        {
            command.CommandId
        };
        var nextState = new StorePreparationState(
            _catalog,
            ShopId,
            CashBalanceCents - totalCostCents,
            nextStock,
            _unitPriceCents,
            nextPurchases,
            nextCommands);

        return new PreparationAttempt<CompletedInventoryPurchase>(true, nextState, completion, null);
    }

    public PreparationAttempt<SetProductPriceCommand> SetPrice(SetProductPriceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CommandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ProductId);

        if (_completedCommandIds.Contains(command.CommandId))
        {
            return Reject<SetProductPriceCommand>(PreparationRejectionReason.DuplicateCommand);
        }

        if (!_catalog.Products.Any(product => string.Equals(
                product.ProductId,
                command.ProductId,
                StringComparison.Ordinal)))
        {
            return Reject<SetProductPriceCommand>(PreparationRejectionReason.UnknownProduct);
        }

        if (command.UnitPriceCents is <= 0 or > long.MaxValue / int.MaxValue)
        {
            return Reject<SetProductPriceCommand>(PreparationRejectionReason.InvalidPrice);
        }

        var nextPrices = new Dictionary<string, long>(_unitPriceCents, StringComparer.Ordinal)
        {
            [command.ProductId] = command.UnitPriceCents
        };
        var nextCommands = new HashSet<string>(_completedCommandIds, StringComparer.Ordinal)
        {
            command.CommandId
        };
        var nextState = new StorePreparationState(
            _catalog,
            ShopId,
            CashBalanceCents,
            _stockByProduct,
            nextPrices,
            _purchases,
            nextCommands);

        return new PreparationAttempt<SetProductPriceCommand>(true, nextState, command, null);
    }

    public PreparationAttempt<StoreState> OpenStore()
    {
        if (_stockByProduct.Count == 0 || _stockByProduct.Values.All(quantity => quantity == 0))
        {
            return Reject<StoreState>(PreparationRejectionReason.NoStock);
        }

        if (_stockByProduct.Any(pair =>
                pair.Value > 0 && !_unitPriceCents.ContainsKey(pair.Key)))
        {
            return Reject<StoreState>(PreparationRejectionReason.MissingPrice);
        }

        var store = new StoreState(
            ShopId,
            isOpen: true,
            _stockByProduct,
            _unitPriceCents,
            CashBalanceCents);
        return new PreparationAttempt<StoreState>(true, this, store, null);
    }

    private PreparationAttempt<T> Reject<T>(PreparationRejectionReason reason) where T : class =>
        new(false, this, null, reason);
}
