using Boxbox.AI.Contracts;
using Boxbox.AI.Customers;
using Boxbox.Application.Customers;
using static Boxbox.Verification.MarketFixtures;
using static Boxbox.Verification.VerificationAssert;

namespace Boxbox.Verification;

internal static class ChoiceProposalChecks
{
    internal static void MismatchedDecisionIdIsRejected()
    {
        VerifyMalformedProposalRejected(
            CustomerChoiceProposalViolationCode.DecisionIdMismatch,
            request => ValidProposal(request, decisionId: $"{request.DecisionId}/forged"));
    }

    internal static void InvalidEvaluationSetsAreRejected()
    {
        VerifyMalformedProposalRejected(
            CustomerChoiceProposalViolationCode.UnknownEvaluationOptionId,
            request => new CustomerChoiceProposal(
                request.DecisionId,
                PlayerOptionId(request),
                [
                    Evaluation(PlayerOptionId(request), 1.0),
                    Evaluation("forged-option", 0.0)
                ]));

        VerifyMalformedProposalRejected(
            CustomerChoiceProposalViolationCode.DuplicateEvaluationOptionId,
            request => new CustomerChoiceProposal(
                request.DecisionId,
                PlayerOptionId(request),
                [
                    Evaluation(PlayerOptionId(request), 0.5),
                    Evaluation(PlayerOptionId(request), 0.5)
                ]));

        VerifyMalformedProposalRejected(
            CustomerChoiceProposalViolationCode.EvaluationSetMismatch,
            request => new CustomerChoiceProposal(
                request.DecisionId,
                PlayerOptionId(request),
                [Evaluation(PlayerOptionId(request), 1.0)]));
    }

    internal static void UnknownSelectedOptionIsRejected()
    {
        VerifyMalformedProposalRejected(
            CustomerChoiceProposalViolationCode.UnknownSelectedOptionId,
            request => ValidProposal(request, selectedOptionId: "forged-option"));
    }

    internal static void ZeroProbabilitySelectionIsRejected()
    {
        VerifyMalformedProposalRejected(
            CustomerChoiceProposalViolationCode.SelectedOptionHasZeroProbability,
            request => new CustomerChoiceProposal(
                request.DecisionId,
                PlayerOptionId(request),
                [
                    Evaluation(PlayerOptionId(request), 0.0),
                    Evaluation(OutsideOptionId(request), 1.0)
                ]));
    }

    internal static void InvalidProbabilitiesAreRejected()
    {
        foreach (var invalidProbability in new[]
                 {
                     double.NaN,
                     double.PositiveInfinity,
                     -0.01,
                     1.01
                 })
        {
            VerifyMalformedProposalRejected(
                CustomerChoiceProposalViolationCode.InvalidProbability,
                request => new CustomerChoiceProposal(
                    request.DecisionId,
                    PlayerOptionId(request),
                    [
                        Evaluation(PlayerOptionId(request), invalidProbability),
                        Evaluation(OutsideOptionId(request), 0.0)
                    ]));
        }
    }

    internal static void InvalidProbabilitySumIsRejected()
    {
        VerifyMalformedProposalRejected(
            CustomerChoiceProposalViolationCode.ProbabilitySumMismatch,
            request => new CustomerChoiceProposal(
                request.DecisionId,
                PlayerOptionId(request),
                [
                    Evaluation(PlayerOptionId(request), 0.4),
                    Evaluation(OutsideOptionId(request), 0.4)
                ]));
    }

    private static void VerifyMalformedProposalRejected(
        CustomerChoiceProposalViolationCode expectedCode,
        Func<CustomerChoiceRequest, CustomerChoiceProposal> malformedProposal)
    {
        const string decisionId = "malformed-proposal-decision";
        var store = NewOpenStore();
        var customer = new CustomerChoiceSession(
            "malformed-proposal-customer",
            new CustomerPreferenceProfile("regular", 200, 150, 150, 200, 300),
            new RejectOnceChoiceEngine(malformedProposal),
            learningRate: 100);
        var service = new CustomerPurchaseService(noPurchaseBaseUtility: 300);

        var violation = Capture<CustomerChoiceProposalViolationException>(() => service.ExecutePlayerStoreOnly(
            customer,
            store,
            decisionId,
            "rice",
            1,
            StandardFeatures(),
            new FixedRandomSource(0.5)));

        Require(violation.Code == expectedCode,
            $"Expected proposal violation {expectedCode}, but received {violation.Code}.");
        Require(store.CashBalanceCents == 1_000,
            "A rejected customer choice proposal changed player cash.");
        Require(store.StockByProduct["rice"] == 5,
            "A rejected customer choice proposal changed player stock.");

        var retry = service.ExecutePlayerStoreOnly(
            customer,
            store,
            decisionId,
            "rice",
            1,
            StandardFeatures(),
            new FixedRandomSource(0.5));

        Require(retry.Status == CustomerPurchaseFlowStatus.SaleCompleted,
            "A rejected proposal consumed the decision id before a valid retry.");
        Require(retry.ResultingStoreState.CashBalanceCents == 1_500,
            "A valid retry did not credit the expected sale value.");
        Require(retry.ResultingStoreState.StockByProduct["rice"] == 4,
            "A valid retry did not debit exactly one stock unit.");
    }

    private static CustomerChoiceProposal ValidProposal(
        CustomerChoiceRequest request,
        string? decisionId = null,
        string? selectedOptionId = null) =>
        new(
            decisionId ?? request.DecisionId,
            selectedOptionId ?? PlayerOptionId(request),
            [
                Evaluation(PlayerOptionId(request), 1.0),
                Evaluation(OutsideOptionId(request), 0.0)
            ]);

    private static ChoiceProposalEvaluation Evaluation(string optionId, double probability) =>
        new(optionId, Utility: 0, probability, Array.Empty<FactorContribution>());

    private static string PlayerOptionId(CustomerChoiceRequest request) =>
        request.AdmissibleOptions.Single(option => option.Kind == ChoiceOptionKind.PurchaseAtShop).Id;

    private static string OutsideOptionId(CustomerChoiceRequest request) =>
        request.AdmissibleOptions.Single(option => option.Kind == ChoiceOptionKind.DoNotPurchase).Id;
}
