namespace Boxbox.Domain.Commerce;

public sealed class SaleAttemptResult
{
    private SaleAttemptResult(
        bool completed,
        StoreState resultingState,
        CompletedSale? sale,
        SaleRejectionReason? rejectionReason)
    {
        Completed = completed;
        ResultingState = resultingState;
        Sale = sale;
        RejectionReason = rejectionReason;
    }

    public bool Completed { get; }
    public StoreState ResultingState { get; }
    public CompletedSale? Sale { get; }
    public SaleRejectionReason? RejectionReason { get; }

    internal static SaleAttemptResult Succeeded(StoreState state, CompletedSale sale) =>
        new(true, state, sale, null);

    internal static SaleAttemptResult Rejected(StoreState unchangedState, SaleRejectionReason reason) =>
        new(false, unchangedState, null, reason);
}
