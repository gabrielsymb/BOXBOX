using System;

namespace BoxBox.Domain
{
    public sealed class ProductDefinition
    {
        public string Id { get; }
        public string NameKey { get; }
        public Money WholesaleCost { get; }
        public Money StartingRetailPrice { get; }
        public Money CompetitorPrice { get; }
        public Money CustomerWillingness { get; }
        public int DemandWeight { get; }
        public int InitialOrderQuantity { get; }
        public int MaximumOrderQuantity { get; }

        public ProductDefinition(
            string id,
            string nameKey,
            long wholesaleCostCents,
            long startingRetailPriceCents,
            long competitorPriceCents,
            long customerWillingnessCents,
            int demandWeight,
            int initialOrderQuantity,
            int maximumOrderQuantity)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Product id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(nameKey))
                throw new ArgumentException("Product name key is required.", nameof(nameKey));
            if (wholesaleCostCents <= 0 || startingRetailPriceCents <= 0 ||
                competitorPriceCents <= 0 || customerWillingnessCents <= 0 ||
                wholesaleCostCents > 1_000_000_000 || startingRetailPriceCents > 1_000_000_000 ||
                competitorPriceCents > 1_000_000_000 || customerWillingnessCents > 1_000_000_000)
                throw new ArgumentOutOfRangeException(nameof(wholesaleCostCents));
            if (demandWeight <= 0 || demandWeight > 1_000_000)
                throw new ArgumentOutOfRangeException(nameof(demandWeight));
            if (maximumOrderQuantity <= 0 || maximumOrderQuantity > 1000)
                throw new ArgumentOutOfRangeException(nameof(maximumOrderQuantity));
            if (initialOrderQuantity < 0 || initialOrderQuantity > maximumOrderQuantity)
                throw new ArgumentOutOfRangeException(nameof(initialOrderQuantity));

            Id = id;
            NameKey = nameKey;
            WholesaleCost = new Money(wholesaleCostCents);
            StartingRetailPrice = new Money(startingRetailPriceCents);
            CompetitorPrice = new Money(competitorPriceCents);
            CustomerWillingness = new Money(customerWillingnessCents);
            DemandWeight = demandWeight;
            InitialOrderQuantity = initialOrderQuantity;
            MaximumOrderQuantity = maximumOrderQuantity;
        }
    }
}
