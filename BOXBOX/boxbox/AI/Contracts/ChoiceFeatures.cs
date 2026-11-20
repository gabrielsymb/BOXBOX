namespace Boxbox.AI.Contracts;

public readonly record struct ChoiceFeatures(
    int PriceAttractiveness,
    int QueueComfort,
    int Convenience,
    int Reputation,
    int Familiarity)
{
    public void Validate()
    {
        ValidateFeature(PriceAttractiveness, nameof(PriceAttractiveness));
        ValidateFeature(QueueComfort, nameof(QueueComfort));
        ValidateFeature(Convenience, nameof(Convenience));
        ValidateFeature(Reputation, nameof(Reputation));
        ValidateFeature(Familiarity, nameof(Familiarity));
    }

    private static void ValidateFeature(int value, string name)
    {
        if (value is < 0 or > 1000)
        {
            throw new ArgumentOutOfRangeException(name, value, "Choice features must be between 0 and 1000.");
        }
    }
}
