using System.Collections.ObjectModel;

namespace Boxbox.Domain.Commerce;

public sealed class StoreState
{
    private readonly IReadOnlyDictionary<string, int> _stockByProduct;
    private readonly IReadOnlyDictionary<string, long> _unitPriceCents;
    private readonly HashSet<string> _completedDecisionIds;

    public StoreState(
        string shopId,
        bool isOpen,
        IReadOnlyDictionary<string, int> stockByProduct,
        IReadOnlyDictionary<string, long> unitPriceCents,
        long cashBalanceCents = 0)
        : this(shopId, isOpen, stockByProduct, unitPriceCents, cashBalanceCents, [])
    {
    }

    private StoreState(
        string shopId,
        bool isOpen,
        IReadOnlyDictionary<string, int> stockByProduct,
        IReadOnlyDictionary<string, long> unitPriceCents,
        long cashBalanceCents,
        IEnumerable<string> completedDecisionIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentNullException.ThrowIfNull(stockByProduct);
        ArgumentNullException.ThrowIfNull(unitPriceCents);
        if (cashBalanceCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cashBalanceCents), "Cash balance cannot be negative.");
        }

        var stock = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (productId, quantity) in stockByProduct)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(productId);
            if (quantity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stockByProduct), "Stock quantity cannot be negative.");
            }

            stock.Add(productId, quantity);
        }

        var prices = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var (productId, priceCents) in unitPriceCents)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(productId);
            if (priceCents <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unitPriceCents), "Unit price must be positive.");
            }

            prices.Add(productId, priceCents);
        }

        ShopId = shopId;
        IsOpen = isOpen;
        _stockByProduct = new ReadOnlyDictionary<string, int>(stock);
        _unitPriceCents = new ReadOnlyDictionary<string, long>(prices);
        CashBalanceCents = cashBalanceCents;
        _completedDecisionIds = new HashSet<string>(completedDecisionIds, StringComparer.Ordinal);
    }

    public string ShopId { get; }
    public bool IsOpen { get; }
    public long CashBalanceCents { get; }
    public IReadOnlyDictionary<string, int> StockByProduct => _stockByProduct;
    public IReadOnlyDictionary<string, long> UnitPriceCentsByProduct => _unitPriceCents;
    public IReadOnlySet<string> CompletedDecisionIds => _completedDecisionIds;

    public static StoreState Restore(
        string shopId,
        bool isOpen,
        IReadOnlyDictionary<string, int> stockByProduct,
        IReadOnlyDictionary<string, long> unitPriceCents,
        long cashBalanceCents,
        IEnumerable<string> completedDecisionIds) => new(
            shopId,
            isOpen,
            stockByProduct,
            unitPriceCents,
            cashBalanceCents,
            completedDecisionIds);

    public StoreState Open() => IsOpen
        ? this
        : new StoreState(
            ShopId,
            isOpen: true,
            _stockByProduct,
            _unitPriceCents,
            CashBalanceCents,
            _completedDecisionIds);

    public StoreState Close() => IsOpen
        ? new StoreState(
            ShopId,
            isOpen: false,
            _stockByProduct,
            _unitPriceCents,
            CashBalanceCents,
            _completedDecisionIds)
        : this;

    public bool CanFulfill(string productId, int quantity) =>
        !string.IsNullOrWhiteSpace(productId) &&
        IsOpen &&
        quantity > 0 &&
        _stockByProduct.TryGetValue(productId, out var stock) &&
        stock >= quantity &&
        _unitPriceCents.ContainsKey(productId);

    public bool TryGetUnitPriceCents(string productId, out long unitPriceCents) =>
        _unitPriceCents.TryGetValue(productId, out unitPriceCents);

    public SaleAttemptResult CompleteSale(SaleCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_completedDecisionIds.Contains(command.DecisionId))
        {
            return SaleAttemptResult.Rejected(this, SaleRejectionReason.DuplicateDecision);
        }

        if (!string.Equals(command.ShopId, ShopId, StringComparison.Ordinal))
        {
            return SaleAttemptResult.Rejected(this, SaleRejectionReason.WrongShop);
        }

        if (!IsOpen)
        {
            return SaleAttemptResult.Rejected(this, SaleRejectionReason.ShopClosed);
        }

        if (!_stockByProduct.TryGetValue(command.ProductId, out var available) ||
            !_unitPriceCents.TryGetValue(command.ProductId, out var unitPriceCents))
        {
            return SaleAttemptResult.Rejected(this, SaleRejectionReason.UnknownProduct);
        }

        if (available < command.Quantity)
        {
            return SaleAttemptResult.Rejected(this, SaleRejectionReason.InsufficientStock);
        }

        var totalCents = checked(unitPriceCents * command.Quantity);
        var newCashBalanceCents = checked(CashBalanceCents + totalCents);
        var newStock = _stockByProduct.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        newStock[command.ProductId] = available - command.Quantity;
        var newCompletedDecisionIds = new HashSet<string>(_completedDecisionIds, StringComparer.Ordinal)
        {
            command.DecisionId
        };
        var nextState = new StoreState(
            ShopId,
            IsOpen,
            newStock,
            _unitPriceCents,
            newCashBalanceCents,
            newCompletedDecisionIds);
        var sale = new CompletedSale(
            command.DecisionId,
            command.CustomerId,
            ShopId,
            command.ProductId,
            command.Quantity,
            unitPriceCents,
            totalCents);

        return SaleAttemptResult.Succeeded(nextState, sale);
    }
}
