using Boxbox.Domain.Commerce;

namespace Boxbox.Application.BusinessDay;

/// <summary>Editable Day 1 order and its confirmed preparation.</summary>
public sealed class GroceryOrderSession
{
    public const string FirstOrderProduct = "milk";
    public const int FirstOrderTarget = 5;
    private const ulong Seed = 20261119;

    private readonly DayOneProductDecision[] _decisions = GroceryDayOneScenario.DefaultDecisions.ToArray();
    private GroceryDayOneScenario.PreparedOrder? _confirmed;

    public GroceryOrderSession() => Decisions = Array.AsReadOnly(_decisions);

    public IReadOnlyList<DayOneProductDecision> Decisions { get; }
    public int ConfirmedUnitCount => _confirmed?.Preparation.StockByProduct.Values.Sum() ?? 0;
    public long DraftCostCents => _decisions.Sum(item => OfferFor(item).UnitCostCents * item.OrderQuantity);
    public bool DraftFitsBudget => DraftCostCents <= GroceryDayOneScenario.OpeningCashCents;
    public bool ReachedFirstTarget =>
        _decisions.Any(item => item.ProductId == FirstOrderProduct && item.OrderQuantity >= FirstOrderTarget);

    public SupplierOffer OfferAt(int index) => OfferFor(_decisions[index]);

    public void CycleSupplier(int index)
    {
        var item = _decisions[index];
        var next = Offers(item.ProductId).First(offer => offer.OfferId != item.SupplierOfferId);
        _decisions[index] = item with
        {
            SupplierOfferId = next.OfferId,
            OrderQuantity = Math.Clamp(item.OrderQuantity, next.MinimumQuantity, next.MaximumQuantity)
        };
    }

    public void ChangeQuantity(int index, int delta)
    {
        var item = _decisions[index];
        var offer = OfferFor(item);
        _decisions[index] = item with
        {
            OrderQuantity = item.OrderQuantity + Math.Clamp(delta, offer.MinimumQuantity - item.OrderQuantity, offer.MaximumQuantity - item.OrderQuantity)
        };
    }

    public long? TryConfirm()
    {
        if (_confirmed is not null || !DraftFitsBudget)
        {
            return null;
        }

        _confirmed = GroceryDayOneScenario.PrepareOrder(_decisions);
        return _confirmed.CashBalanceCents;
    }

    public GroceryTradingDay? TryStart(bool requireConfirmation) =>
        _confirmed is { } prepared
            ? GroceryDayOneScenario.StartPrepared(Seed, prepared)
            : !requireConfirmation && DraftFitsBudget
                ? GroceryDayOneScenario.StartPrepared(Seed, GroceryDayOneScenario.PrepareOrder(_decisions))
                : null;

    public void ResetConfirmation() => _confirmed = null;

    private static SupplierOffer OfferFor(DayOneProductDecision item) =>
        Offers(item.ProductId).Single(offer => offer.OfferId == item.SupplierOfferId);

    private static IEnumerable<SupplierOffer> Offers(string productId) =>
        GroceryDayOneScenario.SupplierOffers.Where(offer => offer.ProductId == productId);
}
