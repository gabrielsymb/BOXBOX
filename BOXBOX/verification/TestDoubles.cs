using Boxbox.AI.Contracts;

namespace Boxbox.Verification;

internal sealed class FixedRandomSource(double value) : IChoiceRandomSource
{
    public double NextUnitDouble() => value;
}

internal sealed class CountingRandomSource(double value) : IChoiceRandomSource
{
    public int Calls { get; private set; }

    public double NextUnitDouble()
    {
        Calls++;
        return value;
    }
}

internal sealed class RejectOnceChoiceEngine(
    Func<CustomerChoiceRequest, CustomerChoiceProposal> malformedProposal) : ICustomerChoiceEngine
{
    private bool _hasRejected;

    public CustomerChoiceProposal Propose(
        CustomerChoiceRequest request,
        IChoiceRandomSource randomSource)
    {
        if (!_hasRejected)
        {
            _hasRejected = true;
            return malformedProposal(request);
        }

        var playerOptionId = request.AdmissibleOptions
            .Single(option => option.Kind == ChoiceOptionKind.PurchaseAtShop)
            .Id;
        var evaluations = request.AdmissibleOptions
            .Select(option => new ChoiceProposalEvaluation(
                option.Id,
                Utility: 0,
                string.Equals(option.Id, playerOptionId, StringComparison.Ordinal) ? 1.0 : 0.0,
                Array.Empty<FactorContribution>()))
            .ToArray();
        return new CustomerChoiceProposal(request.DecisionId, playerOptionId, evaluations);
    }
}

internal sealed class SelectOptionChoiceEngine(string selectedOptionId) : ICustomerChoiceEngine
{
    public CustomerChoiceProposal Propose(
        CustomerChoiceRequest request,
        IChoiceRandomSource randomSource)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(randomSource);
        if (!request.AdmissibleOptions.Any(option => string.Equals(
                option.Id,
                selectedOptionId,
                StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Controlled option is not admissible: {selectedOptionId}");
        }

        var evaluations = request.AdmissibleOptions
            .Select(option => new ChoiceProposalEvaluation(
                option.Id,
                Utility: 0,
                string.Equals(option.Id, selectedOptionId, StringComparison.Ordinal) ? 1.0 : 0.0,
                Array.Empty<FactorContribution>()))
            .ToArray();
        return new CustomerChoiceProposal(request.DecisionId, selectedOptionId, evaluations);
    }
}
