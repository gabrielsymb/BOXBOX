using System.Collections.ObjectModel;

namespace Boxbox.Domain.Catalog;

/// <summary>
/// Immutable authoritative mapping from product ids to category ids.
/// </summary>
public sealed class ProductCatalog
{
    private readonly IReadOnlyList<ProductDefinition> _products;
    private readonly IReadOnlyDictionary<string, ProductDefinition> _productsById;

    public ProductCatalog(IEnumerable<ProductDefinition> products)
    {
        ArgumentNullException.ThrowIfNull(products);

        var productsById = new Dictionary<string, ProductDefinition>(StringComparer.Ordinal);
        foreach (var product in products)
        {
            ArgumentNullException.ThrowIfNull(product);
            if (!productsById.TryAdd(product.ProductId, product))
            {
                throw new ArgumentException(
                    $"Product id '{product.ProductId}' is duplicated.",
                    nameof(products));
            }
        }

        if (productsById.Count == 0)
        {
            throw new ArgumentException(
                "A product catalog must contain at least one product.",
                nameof(products));
        }

        var orderedProducts = productsById.Values
            .OrderBy(static product => product.ProductId, StringComparer.Ordinal)
            .ToList();

        _products = new ReadOnlyCollection<ProductDefinition>(orderedProducts);
        _productsById = new ReadOnlyDictionary<string, ProductDefinition>(productsById);
    }

    public IReadOnlyList<ProductDefinition> Products => _products;

    public ProductDefinition GetRequired(string productId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);

        return _productsById.TryGetValue(productId, out var product)
            ? product
            : throw new KeyNotFoundException($"Unknown product id '{productId}'.");
    }

    public bool IsProductInCategory(string productId, string categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);

        return _productsById.TryGetValue(productId, out var product) &&
               string.Equals(product.CategoryId, categoryId, StringComparison.Ordinal);
    }

    public ProductDefinition RequireProductInCategory(string productId, string categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
        var product = GetRequired(productId);
        if (!string.Equals(product.CategoryId, categoryId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Product '{productId}' does not belong to category '{categoryId}'.",
                nameof(categoryId));
        }

        return product;
    }
}
