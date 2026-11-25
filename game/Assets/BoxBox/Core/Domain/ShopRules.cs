using System;
using System.Collections.Generic;

namespace BoxBox.Domain
{
    public static class ShopRules
    {
        public static ShopState CreateNew(
            IReadOnlyList<ProductDefinition> products,
            Money openingCash,
            int dailyCustomerCount)
        {
            if (products == null || products.Count == 0)
                throw new ArgumentException("At least one product is required.", nameof(products));
            if (openingCash.Cents < 0)
                throw new ArgumentOutOfRangeException(nameof(openingCash));
            if (dailyCustomerCount <= 0 || dailyCustomerCount > 100_000)
                throw new ArgumentOutOfRangeException(nameof(dailyCustomerCount));

            var definitions = new ProductDefinition[products.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < products.Count; i++)
            {
                ProductDefinition product = products[i] ??
                    throw new ArgumentException("Product definitions cannot be null.", nameof(products));
                if (!ids.Add(product.Id))
                    throw new ArgumentException("Product ids must be unique.", nameof(products));
                definitions[i] = product;
            }

            return new ShopState(definitions, openingCash.Cents, dailyCustomerCount);
        }

        public static OperationResult PlaceOrder(ShopState state, IReadOnlyList<int> quantities)
        {
            if (state.Phase != ShopPhase.PlanningOrder)
                return OperationResult.Failure(OperationCode.WrongPhase);
            if (quantities == null || quantities.Count != state.Products.Count)
                return OperationResult.Failure(OperationCode.InvalidOrder);

            long totalCents = 0;
            bool hasQuantity = false;
            for (int i = 0; i < quantities.Count; i++)
            {
                int quantity = quantities[i];
                ProductDefinition product = state.Products[i];
                if (quantity < 0 || quantity > product.MaximumOrderQuantity)
                    return OperationResult.Failure(OperationCode.InvalidOrder);
                if (quantity == 0)
                    continue;

                hasQuantity = true;
                try
                {
                    totalCents = checked(totalCents + checked(product.WholesaleCost.Cents * quantity));
                }
                catch (OverflowException)
                {
                    return OperationResult.Failure(OperationCode.ArithmeticOverflow);
                }
            }

            if (!hasQuantity)
                return OperationResult.Failure(OperationCode.InvalidOrder);
            if (state.Cash.Cents < totalCents)
                return OperationResult.Failure(OperationCode.InsufficientFunds);

            state.Cash = new Money(state.Cash.Cents - totalCents);
            state.SupplierPurchases = new Money(totalCents);
            for (int i = 0; i < quantities.Count; i++)
                state.SetOrder(i, quantities[i]);
            state.Phase = ShopPhase.DrivingToWholesaler;
            return OperationResult.Success();
        }

        public static OperationResult ArriveAtWholesaler(ShopState state)
        {
            return Transition(state, ShopPhase.DrivingToWholesaler, ShopPhase.PickupPending);
        }

        public static OperationResult LoadCargo(ShopState state)
        {
            return Transition(state, ShopPhase.PickupPending, ShopPhase.DrivingToStore);
        }

        public static OperationResult ArriveAtStore(ShopState state)
        {
            return Transition(state, ShopPhase.DrivingToStore, ShopPhase.UnloadPending);
        }

        public static OperationResult UnloadCargo(ShopState state)
        {
            if (state.Phase != ShopPhase.UnloadPending)
                return OperationResult.Failure(OperationCode.WrongPhase);

            var nextInventory = new int[state.Products.Count];
            for (int i = 0; i < state.Products.Count; i++)
            {
                try
                {
                    nextInventory[i] = checked(state.InventoryAtIndex(i) + state.OrderAtIndex(i));
                }
                catch (OverflowException)
                {
                    return OperationResult.Failure(OperationCode.ArithmeticOverflow);
                }
            }

            for (int i = 0; i < state.Products.Count; i++)
            {
                state.SetInventory(i, nextInventory[i]);
                state.SetOrder(i, 0);
            }

            state.Phase = ShopPhase.ReadyToOpen;
            return OperationResult.Success();
        }

        public static OperationResult OpenStore(ShopState state)
        {
            return Transition(state, ShopPhase.ReadyToOpen, ShopPhase.Open);
        }

        public static OperationResult SetRetailPrice(ShopState state, string productId, Money price)
        {
            if (state.Phase != ShopPhase.Open)
                return OperationResult.Failure(OperationCode.WrongPhase);
            if (!state.TryGetProductIndex(productId, out int index))
                return OperationResult.Failure(OperationCode.UnknownProduct);
            if (price.Cents <= 0)
                return OperationResult.Failure(OperationCode.InvalidPrice);

            state.SetRetailPrice(index, price.Cents);
            return OperationResult.Success();
        }

        public static OperationResult ServeCustomer(ShopState state, CustomerDecision decision)
        {
            if (state.Phase != ShopPhase.Open)
                return OperationResult.Failure(OperationCode.WrongPhase);
            if (state.CustomersRemaining <= 0)
                return OperationResult.Failure(OperationCode.NoCustomersRemaining);
            if (!state.TryGetProductIndex(decision.ProductId, out int index))
                return OperationResult.Failure(OperationCode.UnknownProduct);

            ProductDefinition product = state.Products[index];
            int inventory = state.InventoryAtIndex(index);
            Money price = state.RetailPriceAtIndex(index);
            switch (decision.Outcome)
            {
                case CustomerOutcome.BoughtFromPlayer:
                    if (inventory <= 0 || price.Cents > product.CustomerWillingness.Cents)
                        return OperationResult.Failure(OperationCode.InvalidCustomerChoice);
                    return RecordSale(state, index, price);
                case CustomerOutcome.BoughtFromCompetitor:
                    if (product.CompetitorPrice.Cents > product.CustomerWillingness.Cents)
                        return OperationResult.Failure(OperationCode.InvalidCustomerChoice);
                    state.CompetitorChoices++;
                    state.CustomersProcessed++;
                    return OperationResult.Success();
                case CustomerOutcome.Passed:
                    state.PassedCustomers++;
                    state.CustomersProcessed++;
                    return OperationResult.Success();
                default:
                    return OperationResult.Failure(OperationCode.InvalidCustomerChoice);
            }
        }

        public static OperationResult CloseStore(ShopState state)
        {
            if (state.Phase != ShopPhase.Open)
                return OperationResult.Failure(OperationCode.WrongPhase);
            if (state.CustomersRemaining > 0)
                return OperationResult.Failure(OperationCode.NoCustomersRemaining);

            state.Phase = ShopPhase.Closed;
            return OperationResult.Success();
        }

        public static DailyReport CreateReport(ShopState state)
        {
            if (state.Phase != ShopPhase.Closed)
                throw new InvalidOperationException("The business day is not closed.");

            long costOfGoodsSold = 0;
            int unitsSold = 0;
            int remainingInventory = 0;
            for (int i = 0; i < state.Products.Count; i++)
            {
                int sold = state.UnitsSoldAt(i);
                costOfGoodsSold = checked(
                    costOfGoodsSold + checked(state.Products[i].WholesaleCost.Cents * sold));
                unitsSold = checked(unitsSold + sold);
                remainingInventory = checked(remainingInventory + state.InventoryAt(i));
            }

            var cogs = new Money(costOfGoodsSold);
            var grossProfit = new Money(state.SalesRevenue.Cents - costOfGoodsSold);
            return new DailyReport(
                state.OpeningCash,
                state.SupplierPurchases,
                state.SalesRevenue,
                cogs,
                grossProfit,
                state.Cash,
                unitsSold,
                state.CustomersProcessed,
                state.CompetitorChoices,
                state.PassedCustomers,
                remainingInventory);
        }

        private static OperationResult RecordSale(ShopState state, int index, Money price)
        {
            long nextCash;
            long nextRevenue;
            int nextUnitsSold;
            try
            {
                nextCash = checked(state.Cash.Cents + price.Cents);
                nextRevenue = checked(state.SalesRevenue.Cents + price.Cents);
                nextUnitsSold = checked(state.UnitsSoldAt(index) + 1);
            }
            catch (OverflowException)
            {
                return OperationResult.Failure(OperationCode.ArithmeticOverflow);
            }

            state.Cash = new Money(nextCash);
            state.SalesRevenue = new Money(nextRevenue);
            state.SetInventory(index, state.InventoryAtIndex(index) - 1);
            state.SetUnitsSold(index, nextUnitsSold);
            state.CustomersProcessed++;
            return OperationResult.Success();
        }

        private static OperationResult Transition(ShopState state, ShopPhase expected, ShopPhase next)
        {
            if (state.Phase != expected)
                return OperationResult.Failure(OperationCode.WrongPhase);

            state.Phase = next;
            return OperationResult.Success();
        }
    }
}
