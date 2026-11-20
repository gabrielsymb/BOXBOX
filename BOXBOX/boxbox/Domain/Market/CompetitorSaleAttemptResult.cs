namespace Boxbox.Domain.Market;

public sealed class CompetitorSaleAttemptResult
{
    private CompetitorSaleAttemptResult(
        bool completed,
        CompetitorMarketState resultingState,
        CompletedCompetitorSale? sale,
        CompetitorSaleRejectionReason? rejectionReason)
    {
        Completed = completed;
        ResultingState = resultingState;
        Sale = sale;
        RejectionReason = rejectionReason;
    }

    public bool Completed { get; }
    public CompetitorMarketState ResultingState { get; }
    public CompletedCompetitorSale? Sale { get; }
    public CompetitorSaleRejectionReason? RejectionReason { get; }

    internal static CompetitorSaleAttemptResult Succeeded(
        CompetitorMarketState state,
        CompletedCompetitorSale sale) =>
        new(true, state, sale, null);

    internal static CompetitorSaleAttemptResult Rejected(
        CompetitorMarketState unchangedState,
        CompetitorSaleRejectionReason reason) =>
        new(false, unchangedState, null, reason);
}
