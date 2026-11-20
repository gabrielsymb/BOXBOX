namespace Boxbox.Domain.Market;

/// <summary>
/// Stable, publicly observable characteristics of a named competitor.
/// Dynamic or customer-specific perceptions belong to the application layer.
/// </summary>
public sealed class CompetitorPublicAttributes
{
    public CompetitorPublicAttributes(
        string positioningCode,
        int convenience,
        int reputation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(positioningCode);
        ValidateNormalized(convenience, nameof(convenience));
        ValidateNormalized(reputation, nameof(reputation));

        PositioningCode = positioningCode;
        Convenience = convenience;
        Reputation = reputation;
    }

    public string PositioningCode { get; }
    public int Convenience { get; }
    public int Reputation { get; }

    private static void ValidateNormalized(int value, string parameterName)
    {
        if (value is < 0 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Public competitor attributes must be between 0 and 1000.");
        }
    }
}
