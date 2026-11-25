using System;
using BoxBox.Domain;
using UnityEngine;

namespace BoxBox.Infrastructure
{
    public sealed class ProductCatalog
    {
        public ProductDefinition[] Products { get; }
        public Money OpeningCash { get; }
        public int DailyCustomerCount { get; }

        public ProductCatalog(ProductDefinition[] products, Money openingCash, int dailyCustomerCount)
        {
            Products = products;
            OpeningCash = openingCash;
            DailyCustomerCount = dailyCustomerCount;
        }
    }

    public static class ProductCatalogLoader
    {
        private const string ResourcePath = "BoxBox/first-slice-products";

        public static ProductCatalog Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Missing product catalog resource: {ResourcePath}.");

            CatalogDocument document = JsonUtility.FromJson<CatalogDocument>(asset.text);
            if (document == null || document.products == null || document.products.Length == 0)
                throw new InvalidOperationException("The first-slice product catalog is empty or invalid.");

            var products = new ProductDefinition[document.products.Length];
            for (int i = 0; i < products.Length; i++)
            {
                CatalogProduct entry = document.products[i];
                if (entry == null)
                    throw new InvalidOperationException($"Product catalog entry {i} is null.");

                products[i] = new ProductDefinition(
                    entry.id,
                    entry.nameKey,
                    entry.wholesaleCostCents,
                    entry.startingRetailPriceCents,
                    entry.competitorPriceCents,
                    entry.customerWillingnessCents,
                    entry.demandWeight,
                    entry.initialOrderQuantity,
                    entry.maximumOrderQuantity);
            }

            return new ProductCatalog(
                products,
                new Money(document.openingCashCents),
                document.dailyCustomerCount);
        }

        [Serializable]
        private sealed class CatalogDocument
        {
            public long openingCashCents;
            public int dailyCustomerCount;
            public CatalogProduct[] products;
        }

        [Serializable]
        private sealed class CatalogProduct
        {
            public string id;
            public string nameKey;
            public long wholesaleCostCents;
            public long startingRetailPriceCents;
            public long competitorPriceCents;
            public long customerWillingnessCents;
            public int demandWeight;
            public int initialOrderQuantity;
            public int maximumOrderQuantity;
        }
    }
}
