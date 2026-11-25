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

            var state = new ShopState(definitions, openingCash.Cents, dailyCustomerCount);
            RestockWholesaler(state);
            return state;
        }

        public static OperationResult LeaveForWholesaler(ShopState state)
        {
            return Transition(state, ShopPhase.AtStore, ShopPhase.DrivingToWholesaler);
        }

        /// <summary>
        /// Reaching the wholesaler counts from any trip, including driving back to it with goods
        /// already on board to buy more: where the vehicle is decides, not the order of the steps.
        /// </summary>
        public static OperationResult ArriveAtWholesaler(ShopState state)
        {
            if (state.Phase == ShopPhase.DrivingToStore)
                return Transition(state, ShopPhase.DrivingToStore, ShopPhase.AtWholesaler);
            return Transition(state, ShopPhase.DrivingToWholesaler, ShopPhase.AtWholesaler);
        }

        /// <summary>After closing: next day starts at the store, the wholesaler restocks.</summary>
        public static OperationResult StartNextDay(ShopState state)
        {
            OperationResult result = Transition(state, ShopPhase.Closed, ShopPhase.AtStore);
            if (!result.IsSuccess) return result;
            state.Day++;
            state.ResetDailyTotals();
            RestockWholesaler(state);
            return result;
        }

        /// <summary>
        /// Pays at the wholesaler's counter and puts the goods on the vehicle. Can be repeated
        /// while parked there. Same order as Moonforge BuyFromShopCommandHandler: validate
        /// quantities, check the seller's stock, check funds, then apply every change at once.
        /// </summary>
        public static OperationResult BuyAtWholesaler(ShopState state, IReadOnlyList<int> quantities)
        {
            if (state.Phase != ShopPhase.AtWholesaler)
                return OperationResult.Failure(OperationCode.WrongPhase);
            if (quantities == null || quantities.Count != state.Products.Count)
                return OperationResult.Failure(OperationCode.InvalidOrder);

            long totalCents = 0;
            bool hasQuantity = false;
            var nextCargo = new int[quantities.Count];
            try
            {
                for (int i = 0; i < quantities.Count; i++)
                {
                    int quantity = quantities[i];
                    ProductDefinition product = state.Products[i];
                    if (quantity < 0 || quantity > product.MaximumOrderQuantity)
                        return OperationResult.Failure(OperationCode.InvalidOrder);
                    if (quantity > state.WholesalerStockAt(i))
                        return OperationResult.Failure(OperationCode.OutOfStock);

                    hasQuantity |= quantity > 0;
                    totalCents = checked(totalCents + checked(product.WholesaleCost.Cents * quantity));
                    nextCargo[i] = checked(state.CargoAt(i) + quantity);
                }
            }
            catch (OverflowException)
            {
                return OperationResult.Failure(OperationCode.ArithmeticOverflow);
            }

            if (!hasQuantity)
                return OperationResult.Failure(OperationCode.InvalidOrder);
            if (state.Cash.Cents < totalCents)
                return OperationResult.Failure(OperationCode.InsufficientFunds);

            state.Cash = new Money(state.Cash.Cents - totalCents);
            state.SupplierPurchases = new Money(state.SupplierPurchases.Cents + totalCents);
            for (int i = 0; i < quantities.Count; i++)
            {
                state.SetWholesalerStock(i, state.WholesalerStockAt(i) - quantities[i]);
                state.SetCargo(i, nextCargo[i]);
            }
            return OperationResult.Success();
        }

        public static OperationResult LeaveWholesaler(ShopState state)
        {
            return Transition(state, ShopPhase.AtWholesaler, ShopPhase.DrivingToStore);
        }

        /// <summary>
        /// Arriving with an empty vehicle skips the unload step. Coming back before reaching
        /// the wholesaler just cancels the trip.
        /// </summary>
        public static OperationResult ArriveAtStore(ShopState state)
        {
            if (state.Phase == ShopPhase.DrivingToWholesaler)
                return Transition(state, ShopPhase.DrivingToWholesaler, ShopPhase.AtStore);
            return Transition(state, ShopPhase.DrivingToStore,
                state.HasCargo ? ShopPhase.UnloadPending : ShopPhase.AtStore);
        }

        /// <summary>
        /// Moves what physically arrived into the store. delivered[i] is how many units of
        /// product i are still on the vehicle; the rest of the cargo was lost on the way and
        /// its wholesale cost is recorded as a loss (it was already paid for).
        /// </summary>
        public static OperationResult UnloadCargo(ShopState state, IReadOnlyList<int> delivered)
        {
            if (state.Phase != ShopPhase.UnloadPending)
                return OperationResult.Failure(OperationCode.WrongPhase);
            if (delivered == null || delivered.Count != state.Products.Count)
                return OperationResult.Failure(OperationCode.InvalidDelivery);

            var nextInventory = new int[state.Products.Count];
            long lostCents = 0;
            int lostUnits = 0;
            try
            {
                for (int i = 0; i < state.Products.Count; i++)
                {
                    int units = delivered[i];
                    if (units < 0 || units > state.CargoAt(i))
                        return OperationResult.Failure(OperationCode.InvalidDelivery);

                    int lost = state.CargoAt(i) - units;
                    nextInventory[i] = checked(state.InventoryAt(i) + units);
                    lostCents = checked(lostCents + checked(state.Products[i].WholesaleCost.Cents * lost));
                    lostUnits = checked(lostUnits + lost);
                }
                lostCents = checked(state.LostGoods.Cents + lostCents);
                lostUnits = checked(state.UnitsLost + lostUnits);
            }
            catch (OverflowException)
            {
                return OperationResult.Failure(OperationCode.ArithmeticOverflow);
            }

            for (int i = 0; i < state.Products.Count; i++)
            {
                state.SetInventory(i, nextInventory[i]);
                state.SetCargo(i, 0);
            }
            state.LostGoods = new Money(lostCents);
            state.UnitsLost = lostUnits;
            state.Phase = ShopPhase.AtStore;
            return OperationResult.Success();
        }

        /// <summary>
        /// Refills the wholesaler to each product's WholesalerStock, like Moonforge
        /// ShopCommandHelpers.EnsureRestocked resets limited entries to MaxStock.
        /// Called when a business day starts.
        /// </summary>
        public static void RestockWholesaler(ShopState state)
        {
            for (int i = 0; i < state.Products.Count; i++)
                state.SetWholesalerStock(i, state.Products[i].WholesalerStock);
        }

        public static OperationResult OpenStore(ShopState state)
        {
            return Transition(state, ShopPhase.AtStore, ShopPhase.Open);
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
            int inventory = state.InventoryAt(index);
            Money price = state.RetailPriceAt(index);
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
                state.LostGoods,
                state.UnitsLost,
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
            state.SetInventory(index, state.InventoryAt(index) - 1);
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
