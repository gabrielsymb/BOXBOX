namespace Boxbox.AI.Contracts;

public sealed class CustomerPreferenceProfile
{
    public CustomerPreferenceProfile(
        string id,
        int priceWeight,
        int queueWeight,
        int convenienceWeight,
        int reputationWeight,
        int familiarityWeight)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A profile must have a stable id.", nameof(id));
        }

        var weights = new[] { priceWeight, queueWeight, convenienceWeight, reputationWeight, familiarityWeight };
        if (weights.Any(weight => weight is < 0 or > 1000) || weights.Sum() != 1000)
        {
            throw new ArgumentException("Preference weights must be between 0 and 1000 and sum to 1000.");
        }

        Id = id;
        PriceWeight = priceWeight;
        QueueWeight = queueWeight;
        ConvenienceWeight = convenienceWeight;
        ReputationWeight = reputationWeight;
        FamiliarityWeight = familiarityWeight;
    }

    public string Id { get; }
    public int PriceWeight { get; }
    public int QueueWeight { get; }
    public int ConvenienceWeight { get; }
    public int ReputationWeight { get; }
    public int FamiliarityWeight { get; }
}
