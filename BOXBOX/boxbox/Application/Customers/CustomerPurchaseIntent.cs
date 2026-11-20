using Boxbox.AI.Contracts;

namespace Boxbox.Application.Customers;

internal enum CustomerPurchaseIntentKind
{
    DemandUnserved,
    ChoseNotToPurchase,
    ChoseStore
}

/// <summary>
/// Behavior output only. It does not mutate cash, stock, competitors or the
/// opportunity ledger; an authoritative domain command must confirm it.
/// </summary>
internal sealed class CustomerPurchaseIntent
{
    internal CustomerPurchaseIntent(
        CustomerPurchaseIntentKind kind,
        CustomerChoiceResult choice)
    {
        Kind = kind;
        Choice = choice;
    }

    public CustomerPurchaseIntentKind Kind { get; }
    public CustomerChoiceResult Choice { get; }
}
