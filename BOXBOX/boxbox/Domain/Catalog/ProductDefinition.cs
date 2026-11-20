namespace Boxbox.Domain.Catalog;

/// <summary>
/// Stable product identity and its authoritative category. Player-facing names
/// remain localization keys outside this minimal domain contract.
/// </summary>
public sealed class ProductDefinition
{
    public ProductDefinition(string productId, string categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);

        ProductId = productId;
        CategoryId = categoryId;
    }

    public string ProductId { get; }
    public string CategoryId { get; }
}
