using Boxbox.AI.Contracts;
using Boxbox.Domain.Commerce;

namespace Boxbox.Application.Customers;

public enum CustomerPurchaseFlowStatus
{
    ChoseNotToPurchase,
    DemandUnserved,
    ChoseAlternativeStore,
    SaleCompleted,
    SaleRejected
}

public sealed class CustomerPurchaseFlowResult
{
    internal CustomerPurchaseFlowResult(
        CustomerPurchaseFlowStatus status,
        CustomerChoiceResult choice,
        StoreState resultingStoreState,
        CompletedSale? sale,
        SaleRejectionReason? rejectionReason)
    {
        Status = status;
        Choice = choice;
        ResultingStoreState = resultingStoreState;
        Sale = sale;
        RejectionReason = rejectionReason;
    }

    public CustomerPurchaseFlowStatus Status { get; }
    public CustomerChoiceResult Choice { get; }
    public StoreState ResultingStoreState { get; }
    public CompletedSale? Sale { get; }
    public SaleRejectionReason? RejectionReason { get; }
    public string? SelectedShopId => Choice.Selected.Option.ShopId;
}
