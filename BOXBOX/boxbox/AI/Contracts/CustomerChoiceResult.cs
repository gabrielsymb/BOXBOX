namespace Boxbox.AI.Contracts;

public enum ChoiceFactor
{
    Price,
    Queue,
    Convenience,
    Reputation,
    Familiarity
}

public sealed record FactorContribution(ChoiceFactor Factor, int Utility);

public sealed record ChoiceProposalEvaluation(
    string OptionId,
    int Utility,
    double Probability,
    IReadOnlyList<FactorContribution> Contributions);

public sealed class CustomerChoiceProposal
{
    public CustomerChoiceProposal(
        string decisionId,
        string selectedOptionId,
        IReadOnlyCollection<ChoiceProposalEvaluation> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);

        DecisionId = decisionId;
        SelectedOptionId = selectedOptionId;
        Evaluations = Array.AsReadOnly(evaluations.ToArray());
    }

    public string DecisionId { get; }
    public string SelectedOptionId { get; }
    public IReadOnlyList<ChoiceProposalEvaluation> Evaluations { get; }
}

public enum CustomerChoiceProposalViolationCode
{
    NullProposal,
    DecisionIdMismatch,
    NullEvaluation,
    UnknownEvaluationOptionId,
    DuplicateEvaluationOptionId,
    EvaluationSetMismatch,
    UnknownSelectedOptionId,
    SelectedOptionHasZeroProbability,
    InvalidProbability,
    ProbabilitySumMismatch,
    MissingContributions
}

public sealed class CustomerChoiceProposalViolationException : InvalidOperationException
{
    public CustomerChoiceProposalViolationException(
        CustomerChoiceProposalViolationCode code,
        string message,
        string? optionId = null)
        : base(message)
    {
        Code = code;
        OptionId = optionId;
    }

    public CustomerChoiceProposalViolationCode Code { get; }
    public string? OptionId { get; }
}

public sealed record OptionEvaluation(
    ChoiceOption Option,
    int Utility,
    double Probability,
    IReadOnlyList<FactorContribution> Contributions);

public sealed class CustomerChoiceResult
{
    internal CustomerChoiceResult(
        string decisionId,
        OptionEvaluation selected,
        IReadOnlyCollection<OptionEvaluation> evaluations)
    {
        DecisionId = decisionId;
        Selected = selected;
        Evaluations = Array.AsReadOnly(evaluations.ToArray());
    }

    public string DecisionId { get; }
    public OptionEvaluation Selected { get; }
    public IReadOnlyList<OptionEvaluation> Evaluations { get; }
}
