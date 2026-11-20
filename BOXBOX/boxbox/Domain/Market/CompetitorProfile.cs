using System.Collections.ObjectModel;

namespace Boxbox.Domain.Market;

/// <summary>
/// Immutable identity and public positioning of one aggregated competitor.
/// The localization key is resolved by presentation; no translated text is
/// stored in the domain.
/// </summary>
public sealed class CompetitorProfile
{
    private readonly IReadOnlyList<string> _categoryIds;
    private readonly HashSet<string> _categoryLookup;

    public CompetitorProfile(
        string competitorId,
        string nameLocalizationKey,
        IEnumerable<string> categoryIds,
        CompetitorPublicAttributes publicAttributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competitorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameLocalizationKey);
        ArgumentNullException.ThrowIfNull(categoryIds);
        ArgumentNullException.ThrowIfNull(publicAttributes);

        var categories = new List<string>();
        var categoryLookup = new HashSet<string>(StringComparer.Ordinal);
        foreach (var categoryId in categoryIds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
            if (!categoryLookup.Add(categoryId))
            {
                throw new ArgumentException(
                    $"A competitor profile cannot repeat category id '{categoryId}'.",
                    nameof(categoryIds));
            }

            categories.Add(categoryId);
        }

        if (categories.Count == 0)
        {
            throw new ArgumentException(
                "A competitor profile must serve at least one category.",
                nameof(categoryIds));
        }

        categories.Sort(StringComparer.Ordinal);
        CompetitorId = competitorId;
        NameLocalizationKey = nameLocalizationKey;
        PublicAttributes = publicAttributes;
        _categoryIds = new ReadOnlyCollection<string>(categories);
        _categoryLookup = categoryLookup;
    }

    public string CompetitorId { get; }
    public string NameLocalizationKey { get; }
    public CompetitorPublicAttributes PublicAttributes { get; }
    public IReadOnlyList<string> CategoryIds => _categoryIds;

    public bool ServesCategory(string categoryId) =>
        !string.IsNullOrWhiteSpace(categoryId) && _categoryLookup.Contains(categoryId);
}
