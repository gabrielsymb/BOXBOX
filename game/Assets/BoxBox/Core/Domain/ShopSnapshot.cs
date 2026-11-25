using System.Collections.Generic;

namespace BoxBox.Domain
{
    public readonly struct ProductQuote
    {
        public string ProductId { get; }
        public int DemandWeight { get; }
        public int Inventory { get; }
        public Money RetailPrice { get; }
        public Money CompetitorPrice { get; }
        public Money CustomerWillingness { get; }

        public ProductQuote(
            string productId,
            int demandWeight,
            int inventory,
            Money retailPrice,
            Money competitorPrice,
            Money customerWillingness)
        {
            ProductId = productId;
            DemandWeight = demandWeight;
            Inventory = inventory;
            RetailPrice = retailPrice;
            CompetitorPrice = competitorPrice;
            CustomerWillingness = customerWillingness;
        }
    }

    public sealed class ShopSnapshot
    {
        public IReadOnlyList<ProductQuote> Products { get; }
        public int CustomersRemaining { get; }

        public ShopSnapshot(IReadOnlyList<ProductQuote> products, int customersRemaining)
        {
            Products = products;
            CustomersRemaining = customersRemaining;
        }
    }
}
