using Boxbox.AI.Contracts;

namespace Boxbox.AI.Customers;

public sealed class SoftmaxCustomerChoiceEngine : ICustomerChoiceEngine
{
    private readonly int _temperature;

    public SoftmaxCustomerChoiceEngine(int temperature)
    {
        if (temperature is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 1 and 1000.");
        }

        _temperature = temperature;
    }

    public CustomerChoiceProposal Propose(CustomerChoiceRequest request, IChoiceRandomSource randomSource)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(randomSource);
        ValidateOptions(request.AdmissibleOptions);

        var profile = request.Profile;
        var scoredOptions = request.AdmissibleOptions
            .OrderBy(option => option.Id, StringComparer.Ordinal)
            .Select(option => Score(option, profile))
            .ToArray();

        var maximumUtility = scoredOptions.Max(option => option.Utility);
        var rawWeights = scoredOptions
            .Select(option => Math.Exp((option.Utility - maximumUtility) / (double)_temperature))
            .ToArray();
        var totalWeight = rawWeights.Sum();

        if (!double.IsFinite(totalWeight) || totalWeight <= 0)
        {
            throw new InvalidOperationException("Customer choice produced invalid selection weights.");
        }

        var evaluations = scoredOptions
            .Select((option, index) => new ChoiceProposalEvaluation(
                option.Option.Id,
                option.Utility,
                rawWeights[index] / totalWeight,
                option.Contributions))
            .ToArray();

        var roll = randomSource.NextUnitDouble();
        if (!double.IsFinite(roll) || roll is < 0 or >= 1)
        {
            throw new InvalidOperationException("Random source must return a finite value in [0, 1).");
        }

        var threshold = roll * totalWeight;
        var cumulativeWeight = 0.0;
        var selectedIndex = evaluations.Length - 1;
        for (var index = 0; index < rawWeights.Length; index++)
        {
            cumulativeWeight += rawWeights[index];
            if (threshold < cumulativeWeight)
            {
                selectedIndex = index;
                break;
            }
        }

        return new CustomerChoiceProposal(
            request.DecisionId,
            evaluations[selectedIndex].OptionId,
            evaluations);
    }

    private static OptionScore Score(ChoiceOption option, CustomerPreferenceProfile profile)
    {
        var features = option.Features;
        var contributions = new[]
        {
            new FactorContribution(ChoiceFactor.Price, profile.PriceWeight * features.PriceAttractiveness / 1000),
            new FactorContribution(ChoiceFactor.Queue, profile.QueueWeight * features.QueueComfort / 1000),
            new FactorContribution(ChoiceFactor.Convenience, profile.ConvenienceWeight * features.Convenience / 1000),
            new FactorContribution(ChoiceFactor.Reputation, profile.ReputationWeight * features.Reputation / 1000),
            new FactorContribution(ChoiceFactor.Familiarity, profile.FamiliarityWeight * features.Familiarity / 1000)
        };

        return new OptionScore(
            option,
            option.BaseUtility + contributions.Sum(contribution => contribution.Utility),
            Array.AsReadOnly(contributions));
    }

    private static void ValidateOptions(IReadOnlyList<ChoiceOption> options)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var outsideOptions = 0;
        foreach (var option in options)
        {
            if (!ids.Add(option.Id))
            {
                throw new ArgumentException($"Duplicate option id: {option.Id}", nameof(options));
            }

            if (option.Kind == ChoiceOptionKind.DoNotPurchase)
            {
                outsideOptions++;
            }
        }

        if (outsideOptions != 1)
        {
            throw new ArgumentException("Exactly one do-not-purchase option must be provided.", nameof(options));
        }
    }

    private sealed record OptionScore(
        ChoiceOption Option,
        int Utility,
        IReadOnlyList<FactorContribution> Contributions);
}
