using System;
using BoxBox.Application;
using BoxBox.Domain;

namespace BoxBox.Simulation
{
    public sealed class SeededShopperPolicy : IShopperPolicy
    {
        private uint _state;

        public SeededShopperPolicy(uint seed)
        {
            if (seed == 0)
                throw new ArgumentOutOfRangeException(nameof(seed));
            _state = seed;
        }

        public CustomerDecision Decide(ShopSnapshot shop)
        {
            if (shop == null || shop.Products.Count == 0)
                throw new ArgumentException("A non-empty shop snapshot is required.", nameof(shop));

            ProductQuote selectedProduct = SelectProduct(shop);
            return SelectOutcome(selectedProduct);
        }

        private ProductQuote SelectProduct(ShopSnapshot shop)
        {
            int totalWeight = 0;
            for (int i = 0; i < shop.Products.Count; i++)
                totalWeight = checked(totalWeight + shop.Products[i].DemandWeight);

            int draw = NextInt(totalWeight);
            for (int i = 0; i < shop.Products.Count; i++)
            {
                ProductQuote quote = shop.Products[i];
                if (draw < quote.DemandWeight)
                    return quote;
                draw -= quote.DemandWeight;
            }

            throw new InvalidOperationException("Weighted product selection did not resolve.");
        }

        private CustomerDecision SelectOutcome(ProductQuote product)
        {
            int playerWeight = GetOfferWeight(
                product.Inventory > 0,
                product.RetailPrice.Cents,
                product.CustomerWillingness.Cents);
            int competitorWeight = GetOfferWeight(
                true,
                product.CompetitorPrice.Cents,
                product.CustomerWillingness.Cents);
            const int passWeight = 50;
            int totalWeight = checked(playerWeight + competitorWeight + passWeight);
            int draw = NextInt(totalWeight);

            if (draw < playerWeight)
                return new CustomerDecision(product.ProductId, CustomerOutcome.BoughtFromPlayer);
            draw -= playerWeight;

            if (draw < competitorWeight)
                return new CustomerDecision(product.ProductId, CustomerOutcome.BoughtFromCompetitor);

            return new CustomerDecision(product.ProductId, CustomerOutcome.Passed);
        }

        private static int GetOfferWeight(bool available, long priceCents, long willingnessCents)
        {
            if (!available || priceCents > willingnessCents)
                return 0;

            return checked((int)(willingnessCents - priceCents + 1));
        }

        private int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));

            uint value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return (int)(value % (uint)exclusiveMaximum);
        }
    }
}
