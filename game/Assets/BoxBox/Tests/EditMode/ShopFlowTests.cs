using BoxBox.Application;
using BoxBox.Domain;
using BoxBox.Simulation;
using NUnit.Framework;

namespace BoxBox.Tests
{
    public sealed class ShopFlowTests
    {
        [Test]
        public void InsufficientFundsRejectsOrderWithoutChangingState()
        {
            ShopState state = CreateState(1000, 3);
            OperationResult result = ShopRules.PlaceOrder(state, new[] { 6, 0 });

            Assert.That(result.Code, Is.EqualTo(OperationCode.InsufficientFunds));
            Assert.That(state.Cash.Cents, Is.EqualTo(1000));
            Assert.That(state.SupplierPurchases.Cents, Is.Zero);
            Assert.That(state.Phase, Is.EqualTo(ShopPhase.PlanningOrder));
            Assert.That(state.InventoryAt(0), Is.Zero);
        }

        [Test]
        public void CargoBecomesInventoryOnlyAfterConfirmedUnload()
        {
            ShopState state = CreateState(5000, 2);
            Assert.That(ShopRules.PlaceOrder(state, new[] { 2, 1 }).IsSuccess, Is.True);
            Assert.That(ShopRules.ArriveAtWholesaler(state).IsSuccess, Is.True);
            Assert.That(ShopRules.LoadCargo(state).IsSuccess, Is.True);
            Assert.That(state.InventoryAt(0), Is.Zero);
            Assert.That(ShopRules.ArriveAtStore(state).IsSuccess, Is.True);
            Assert.That(ShopRules.UnloadCargo(state).IsSuccess, Is.True);
            Assert.That(state.InventoryAt(0), Is.EqualTo(2));

            OperationResult duplicateUnload = ShopRules.UnloadCargo(state);
            Assert.That(duplicateUnload.Code, Is.EqualTo(OperationCode.WrongPhase));
            Assert.That(state.InventoryAt(0), Is.EqualTo(2));
        }

        [Test]
        public void CustomerCannotBuyStockThatDoesNotExist()
        {
            ShopState state = CreateReadyToOpenState();
            Assert.That(ShopRules.OpenStore(state).IsSuccess, Is.True);
            OperationResult result = ShopRules.ServeCustomer(
                state,
                new CustomerDecision("water", CustomerOutcome.BoughtFromPlayer));

            Assert.That(result.Code, Is.EqualTo(OperationCode.InvalidCustomerChoice));
            Assert.That(state.Cash.Cents, Is.EqualTo(4700));
            Assert.That(state.CustomersProcessed, Is.Zero);
        }

        [Test]
        public void SameSeedAndCommandsProduceSameDailyReport()
        {
            DailyReport first = RunDay(7231);
            DailyReport second = RunDay(7231);

            Assert.That(second.ClosingCash.Cents, Is.EqualTo(first.ClosingCash.Cents));
            Assert.That(second.SalesRevenue.Cents, Is.EqualTo(first.SalesRevenue.Cents));
            Assert.That(second.UnitsSold, Is.EqualTo(first.UnitsSold));
            Assert.That(
                first.OpeningCash.Cents - first.SupplierPurchases.Cents + first.SalesRevenue.Cents,
                Is.EqualTo(first.ClosingCash.Cents));
        }

        private static ShopState CreateState(long cash, int customers)
        {
            return ShopRules.CreateNew(new[]
            {
                Product("water", 200, 400, 350, 600, 4),
                Product("bread", 300, 500, 450, 700, 2)
            }, new Money(cash), customers);
        }

        private static ProductDefinition Product(
            string id,
            long cost,
            long price,
            long competitor,
            long willingness,
            int demandWeight)
        {
            return new ProductDefinition(id, "product." + id, cost, price, competitor, willingness, demandWeight, 2, 10);
        }

        private static ShopState CreateReadyToOpenState()
        {
            ShopState state = CreateState(5000, 2);
            Assert.That(ShopRules.PlaceOrder(state, new[] { 0, 1 }).IsSuccess, Is.True);
            Assert.That(ShopRules.ArriveAtWholesaler(state).IsSuccess, Is.True);
            Assert.That(ShopRules.LoadCargo(state).IsSuccess, Is.True);
            Assert.That(ShopRules.ArriveAtStore(state).IsSuccess, Is.True);
            Assert.That(ShopRules.UnloadCargo(state).IsSuccess, Is.True);
            return state;
        }

        private static DailyReport RunDay(uint seed)
        {
            ShopState state = CreateState(10000, 8);
            var session = new FirstSliceSession(state, new SeededShopperPolicy(seed));
            Assert.That(session.PlaceOrder(new[] { 2, 2 }).IsSuccess, Is.True);
            Assert.That(session.ArriveAtWholesaler().IsSuccess, Is.True);
            Assert.That(session.LoadCargo().IsSuccess, Is.True);
            Assert.That(session.ArriveAtStore().IsSuccess, Is.True);
            Assert.That(session.UnloadCargo().IsSuccess, Is.True);
            Assert.That(session.OpenStore().IsSuccess, Is.True);

            for (int i = 0; i < state.DailyCustomerCount; i++)
                Assert.That(session.ServeNextCustomer().IsSuccess, Is.True);

            Assert.That(session.CloseStore().IsSuccess, Is.True);
            return session.CreateReport();
        }
    }
}
