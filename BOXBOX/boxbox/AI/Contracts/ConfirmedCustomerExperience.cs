namespace Boxbox.AI.Contracts;

public sealed class ConfirmedCustomerExperience
{
    public ConfirmedCustomerExperience(string decisionId, string shopId, int satisfaction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        if (satisfaction is < -1000 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(satisfaction),
                satisfaction,
                "Satisfaction must be between -1000 and 1000.");
        }

        DecisionId = decisionId;
        ShopId = shopId;
        Satisfaction = satisfaction;
    }

    public string DecisionId { get; }
    public string ShopId { get; }
    public int Satisfaction { get; }
}
