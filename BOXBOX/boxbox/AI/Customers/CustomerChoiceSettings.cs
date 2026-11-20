using Boxbox.AI.Contracts;

namespace Boxbox.AI.Customers;

public sealed class CustomerChoiceSettings
{
    public int Version { get; init; }
    public int Temperature { get; init; }
    public int NoPurchaseBaseUtility { get; init; }
    public int ExperienceLearningRate { get; init; }
    public List<CustomerArchetypeSettings> Profiles { get; init; } = [];

    public void Validate()
    {
        if (Version <= 0)
        {
            throw new InvalidDataException("Customer choice settings require a positive version.");
        }

        if (Temperature is < 1 or > 1000)
        {
            throw new InvalidDataException("Temperature must be between 1 and 1000.");
        }

        if (NoPurchaseBaseUtility is < -1000 or > 1000)
        {
            throw new InvalidDataException("No-purchase utility must be between -1000 and 1000.");
        }

        if (ExperienceLearningRate is < 0 or > 1000)
        {
            throw new InvalidDataException("Experience learning rate must be between 0 and 1000.");
        }

        if (Profiles.Count == 0)
        {
            throw new InvalidDataException("At least one customer profile is required.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in Profiles)
        {
            _ = profile.ToProfile();
            if (!ids.Add(profile.Id))
            {
                throw new InvalidDataException($"Duplicate customer profile id: {profile.Id}");
            }
        }
    }
}

public sealed class CustomerArchetypeSettings
{
    public string Id { get; init; } = string.Empty;
    public int PriceWeight { get; init; }
    public int QueueWeight { get; init; }
    public int ConvenienceWeight { get; init; }
    public int ReputationWeight { get; init; }
    public int FamiliarityWeight { get; init; }

    public CustomerPreferenceProfile ToProfile() => new(
        Id,
        PriceWeight,
        QueueWeight,
        ConvenienceWeight,
        ReputationWeight,
        FamiliarityWeight);
}
