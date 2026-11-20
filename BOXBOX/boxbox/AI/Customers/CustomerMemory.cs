namespace Boxbox.AI.Customers;

public sealed class CustomerMemory
{
    private readonly Dictionary<string, int> _storeAffinity = new(StringComparer.Ordinal);

    public int FamiliarityFeature(string shopId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        var affinity = _storeAffinity.GetValueOrDefault(shopId);
        return 500 + affinity / 2;
    }

    public void RecordExperience(string shopId, int satisfaction, int learningRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        if (satisfaction is < -1000 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(satisfaction), "Satisfaction must be between -1000 and 1000.");
        }

        if (learningRate is < 0 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(learningRate), "Learning rate must be between 0 and 1000.");
        }

        var current = _storeAffinity.GetValueOrDefault(shopId);
        var adjustment = satisfaction * learningRate / 1000;
        _storeAffinity[shopId] = Math.Clamp(current + adjustment, -1000, 1000);
    }
}
