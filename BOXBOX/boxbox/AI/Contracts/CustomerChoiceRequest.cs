namespace Boxbox.AI.Contracts;

public sealed class CustomerChoiceRequest
{
    public CustomerChoiceRequest(
        string decisionId,
        string customerId,
        CustomerPreferenceProfile profile,
        IReadOnlyCollection<ChoiceOption> admissibleOptions)
    {
        if (string.IsNullOrWhiteSpace(decisionId))
        {
            throw new ArgumentException("A decision must have a stable id.", nameof(decisionId));
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new ArgumentException("A customer must have a stable id.", nameof(customerId));
        }

        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(admissibleOptions);
        if (admissibleOptions.Count == 0)
        {
            throw new ArgumentException("At least one admissible option is required.", nameof(admissibleOptions));
        }

        var copiedOptions = admissibleOptions.ToArray();
        var optionIds = new HashSet<string>(StringComparer.Ordinal);
        var outsideOptions = 0;
        foreach (var option in copiedOptions)
        {
            ArgumentNullException.ThrowIfNull(option);
            if (!optionIds.Add(option.Id))
            {
                throw new ArgumentException(
                    $"Duplicate admissible option id: {option.Id}",
                    nameof(admissibleOptions));
            }

            if (option.Kind == ChoiceOptionKind.DoNotPurchase)
            {
                outsideOptions++;
            }
        }

        if (outsideOptions != 1)
        {
            throw new ArgumentException(
                "Exactly one do-not-purchase option is required.",
                nameof(admissibleOptions));
        }

        DecisionId = decisionId;
        CustomerId = customerId;
        Profile = profile;
        AdmissibleOptions = Array.AsReadOnly(copiedOptions);
    }

    public string DecisionId { get; }
    public string CustomerId { get; }
    public CustomerPreferenceProfile Profile { get; }
    public IReadOnlyList<ChoiceOption> AdmissibleOptions { get; }
}
