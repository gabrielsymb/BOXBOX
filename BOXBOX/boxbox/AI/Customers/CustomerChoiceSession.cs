using Boxbox.AI.Contracts;

namespace Boxbox.AI.Customers;

public sealed class CustomerChoiceSession
{
    private const double ProbabilitySumTolerance = 1e-9;
    private readonly string _customerId;
    private readonly CustomerPreferenceProfile _profile;
    private readonly ICustomerChoiceEngine _engine;
    private readonly int _learningRate;
    private readonly CustomerMemory _memory = new();
    private readonly Dictionary<string, string?> _decisions = new(StringComparer.Ordinal);
    private readonly HashSet<string> _confirmedDecisions = new(StringComparer.Ordinal);

    public string CustomerId => _customerId;

    public CustomerChoiceSession(
        string customerId,
        CustomerPreferenceProfile profile,
        ICustomerChoiceEngine engine,
        int learningRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(engine);
        if (learningRate is < 0 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(learningRate), "Learning rate must be between 0 and 1000.");
        }

        _customerId = customerId;
        _profile = profile;
        _engine = engine;
        _learningRate = learningRate;
    }

    public CustomerChoiceResult Decide(
        string decisionId,
        IReadOnlyCollection<ChoiceOption> admissibleOptions,
        IChoiceRandomSource randomSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentNullException.ThrowIfNull(admissibleOptions);
        ArgumentNullException.ThrowIfNull(randomSource);
        if (_decisions.ContainsKey(decisionId))
        {
            throw new ArgumentException($"Decision id has already been used: {decisionId}", nameof(decisionId));
        }

        var personalizedOptions = admissibleOptions
            .Select(PersonalizeFamiliarity)
            .ToArray();
        var request = new CustomerChoiceRequest(decisionId, _customerId, _profile, personalizedOptions);
        var proposal = _engine.Propose(request, randomSource);
        var result = ValidateAndCanonicalize(request, proposal);
        _decisions.Add(
            decisionId,
            result.Selected.Option.Kind == ChoiceOptionKind.PurchaseAtShop
                ? result.Selected.Option.ShopId
                : null);
        return result;
    }

    private static CustomerChoiceResult ValidateAndCanonicalize(
        CustomerChoiceRequest request,
        CustomerChoiceProposal? proposal)
    {
        if (proposal is null)
        {
            throw Violation(
                CustomerChoiceProposalViolationCode.NullProposal,
                "Customer choice engine returned no proposal.");
        }

        if (!string.Equals(proposal.DecisionId, request.DecisionId, StringComparison.Ordinal))
        {
            throw Violation(
                CustomerChoiceProposalViolationCode.DecisionIdMismatch,
                "Customer choice proposal decision id does not match the request.");
        }

        var optionsById = request.AdmissibleOptions.ToDictionary(
            option => option.Id,
            StringComparer.Ordinal);
        var evaluationsById = new Dictionary<string, ChoiceProposalEvaluation>(StringComparer.Ordinal);
        var probabilitySum = 0.0;

        foreach (var evaluation in proposal.Evaluations)
        {
            if (evaluation is null)
            {
                throw Violation(
                    CustomerChoiceProposalViolationCode.NullEvaluation,
                    "Customer choice proposal contains a null evaluation.");
            }

            if (string.IsNullOrWhiteSpace(evaluation.OptionId) ||
                !optionsById.ContainsKey(evaluation.OptionId))
            {
                throw Violation(
                    CustomerChoiceProposalViolationCode.UnknownEvaluationOptionId,
                    "Customer choice proposal evaluated an option outside the admissible set.",
                    evaluation.OptionId);
            }

            if (!evaluationsById.TryAdd(evaluation.OptionId, evaluation))
            {
                throw Violation(
                    CustomerChoiceProposalViolationCode.DuplicateEvaluationOptionId,
                    "Customer choice proposal evaluated the same option more than once.",
                    evaluation.OptionId);
            }

            if (!double.IsFinite(evaluation.Probability) || evaluation.Probability is < 0 or > 1)
            {
                throw Violation(
                    CustomerChoiceProposalViolationCode.InvalidProbability,
                    "Customer choice proposal probability must be finite and between zero and one.",
                    evaluation.OptionId);
            }

            if (evaluation.Contributions is null)
            {
                throw Violation(
                    CustomerChoiceProposalViolationCode.MissingContributions,
                    "Customer choice proposal evaluation must include its factor contributions.",
                    evaluation.OptionId);
            }

            probabilitySum += evaluation.Probability;
        }

        if (evaluationsById.Count != optionsById.Count)
        {
            throw Violation(
                CustomerChoiceProposalViolationCode.EvaluationSetMismatch,
                "Customer choice proposal must evaluate every admissible option exactly once.");
        }

        if (string.IsNullOrWhiteSpace(proposal.SelectedOptionId) ||
            !optionsById.ContainsKey(proposal.SelectedOptionId))
        {
            throw Violation(
                CustomerChoiceProposalViolationCode.UnknownSelectedOptionId,
                "Customer choice proposal selected an option outside the admissible set.",
                proposal.SelectedOptionId);
        }

        if (evaluationsById[proposal.SelectedOptionId].Probability <= 0)
        {
            throw Violation(
                CustomerChoiceProposalViolationCode.SelectedOptionHasZeroProbability,
                "Customer choice proposal cannot select an option with zero probability.",
                proposal.SelectedOptionId);
        }

        if (!double.IsFinite(probabilitySum) || Math.Abs(probabilitySum - 1.0) > ProbabilitySumTolerance)
        {
            throw Violation(
                CustomerChoiceProposalViolationCode.ProbabilitySumMismatch,
                "Customer choice proposal probabilities must sum to one.");
        }

        var canonicalEvaluations = request.AdmissibleOptions
            .OrderBy(option => option.Id, StringComparer.Ordinal)
            .Select(option =>
            {
                var evaluation = evaluationsById[option.Id];
                return new OptionEvaluation(
                    option,
                    evaluation.Utility,
                    evaluation.Probability,
                    Array.AsReadOnly(evaluation.Contributions.ToArray()));
            })
            .ToArray();
        var selected = canonicalEvaluations.Single(
            evaluation => string.Equals(
                evaluation.Option.Id,
                proposal.SelectedOptionId,
                StringComparison.Ordinal));

        return new CustomerChoiceResult(request.DecisionId, selected, canonicalEvaluations);
    }

    private static CustomerChoiceProposalViolationException Violation(
        CustomerChoiceProposalViolationCode code,
        string message,
        string? optionId = null) =>
        new(code, message, optionId);

    public void RecordConfirmedExperience(ConfirmedCustomerExperience experience)
    {
        ArgumentNullException.ThrowIfNull(experience);
        if (!_decisions.TryGetValue(experience.DecisionId, out var selectedShopId))
        {
            throw new ArgumentException(
                $"No completed decision exists with id: {experience.DecisionId}",
                nameof(experience));
        }

        if (selectedShopId is null ||
            !string.Equals(selectedShopId, experience.ShopId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Experience must match the shop selected in the completed decision.");
        }

        if (_confirmedDecisions.Contains(experience.DecisionId))
        {
            throw new InvalidOperationException("Experience for this decision has already been recorded.");
        }

        _memory.RecordExperience(experience.ShopId, experience.Satisfaction, _learningRate);
        _confirmedDecisions.Add(experience.DecisionId);
    }

    private ChoiceOption PersonalizeFamiliarity(ChoiceOption option)
    {
        if (option.Kind != ChoiceOptionKind.PurchaseAtShop)
        {
            return option;
        }

        var shopId = option.ShopId ?? throw new InvalidOperationException("Purchase option is missing a shop id.");
        var features = option.Features with
        {
            Familiarity = _memory.FamiliarityFeature(shopId)
        };
        return new ChoiceOption(option.Id, option.Kind, option.BaseUtility, shopId, features);
    }
}
