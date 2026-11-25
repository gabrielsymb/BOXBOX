using BoxBox.Application;
using BoxBox.Domain;
using BoxBox.Simulation;
using NUnit.Framework;

namespace BoxBox.Tests
{
    public sealed class ShopFlowTests
    {
        [Test]
        public void InsufficientFundsRejectsPurchaseWithoutChangingState()
        {
            ShopState state = AtWholesaler(CreateState(1000, 3));
            OperationResult result = ShopRules.BuyAtWholesaler(state, new[] { 6, 0 });

            Assert.That(result.Code, Is.EqualTo(OperationCode.InsufficientFunds));
            Assert.That(state.Cash.Cents, Is.EqualTo(1000));
            Assert.That(state.SupplierPurchases.Cents, Is.Zero);
            Assert.That(state.CargoAt(0), Is.Zero);
            Assert.That(state.WholesalerStockAt(0), Is.EqualTo(20));
        }

        [Test]
        public void CannotBuyBeforeReachingTheWholesaler()
        {
            ShopState state = CreateState(5000, 2);
            Assert.That(ShopRules.BuyAtWholesaler(state, new[] { 1, 0 }).Code, Is.EqualTo(OperationCode.WrongPhase));
            Assert.That(state.Cash.Cents, Is.EqualTo(5000));
        }

        [Test]
        public void WholesalerStockIsLimitedAndAtomic()
        {
            ShopState state = AtWholesaler(CreateState(100_000, 2));
            Assert.That(ShopRules.BuyAtWholesaler(state, new[] { 8, 0 }).IsSuccess, Is.True);
            Assert.That(ShopRules.BuyAtWholesaler(state, new[] { 8, 0 }).IsSuccess, Is.True);

            OperationResult result = ShopRules.BuyAtWholesaler(state, new[] { 5, 3 });
            Assert.That(result.Code, Is.EqualTo(OperationCode.OutOfStock));
            Assert.That(state.WholesalerStockAt(0), Is.EqualTo(4));
            Assert.That(state.WholesalerStockAt(1), Is.EqualTo(20), "rejected purchase must not touch any product");
            Assert.That(state.CargoAt(0), Is.EqualTo(16));
            Assert.That(state.Cash.Cents, Is.EqualTo(100_000 - 16 * 200));
        }

        [Test]
        public void OnlyDeliveredCargoBecomesInventoryAndTheRestIsALoss()
        {
            ShopState state = AtWholesaler(CreateState(5000, 2));
            Assert.That(ShopRules.BuyAtWholesaler(state, new[] { 4, 2 }).IsSuccess, Is.True);
            DriveToStore(state);
            Assert.That(state.Phase, Is.EqualTo(ShopPhase.UnloadPending));
            Assert.That(state.InventoryAt(0), Is.Zero, "cargo is not stock until unloaded");

            Assert.That(ShopRules.UnloadCargo(state, new[] { 3, 2 }).IsSuccess, Is.True);
            Assert.That(state.InventoryAt(0), Is.EqualTo(3));
            Assert.That(state.InventoryAt(1), Is.EqualTo(2));
            Assert.That(state.UnitsLost, Is.EqualTo(1));
            Assert.That(state.LostGoods.Cents, Is.EqualTo(200));
            Assert.That(state.HasCargo, Is.False);
            Assert.That(state.Phase, Is.EqualTo(ShopPhase.AtStore));

            Assert.That(ShopRules.UnloadCargo(state, new[] { 3, 2 }).Code, Is.EqualTo(OperationCode.WrongPhase));
            Assert.That(state.InventoryAt(0), Is.EqualTo(3));
        }

        [Test]
        public void CannotDeliverMoreThanWasCarried()
        {
            ShopState state = AtWholesaler(CreateState(5000, 2));
            Assert.That(ShopRules.BuyAtWholesaler(state, new[] { 2, 0 }).IsSuccess, Is.True);
            DriveToStore(state);

            Assert.That(ShopRules.UnloadCargo(state, new[] { 3, 0 }).Code, Is.EqualTo(OperationCode.InvalidDelivery));
            Assert.That(state.InventoryAt(0), Is.Zero);
            Assert.That(state.CargoAt(0), Is.EqualTo(2));
        }

        [Test]
        public void EmptyTripSkipsUnloadAndSecondTripIsAllowed()
        {
            ShopState state = AtWholesaler(CreateState(5000, 2));
            DriveToStore(state);
            Assert.That(state.Phase, Is.EqualTo(ShopPhase.AtStore));

            AtWholesaler(state);
            Assert.That(ShopRules.BuyAtWholesaler(state, new[] { 1, 1 }).IsSuccess, Is.True);
            DriveToStore(state);
            Assert.That(ShopRules.UnloadCargo(state, new[] { 1, 1 }).IsSuccess, Is.True);
            Assert.That(state.InventoryAt(1), Is.EqualTo(1));
        }

        [Test]
        public void ReturningBeforeReachingTheWholesalerCancelsTheTrip()
        {
            ShopState state = CreateState(5000, 2);
            Assert.That(ShopRules.LeaveForWholesaler(state).IsSuccess, Is.True);
            Assert.That(ShopRules.ArriveAtStore(state).IsSuccess, Is.True);
            Assert.That(state.Phase, Is.EqualTo(ShopPhase.AtStore));
        }

        [Test]
        public void CustomerCannotBuyStockThatDoesNotExist()
        {
            ShopState state = CreateState(5000, 2);
            Assert.That(ShopRules.OpenStore(state).IsSuccess, Is.True);
            OperationResult result = ShopRules.ServeCustomer(
                state,
                new CustomerDecision("water", CustomerOutcome.BoughtFromPlayer));

            Assert.That(result.Code, Is.EqualTo(OperationCode.InvalidCustomerChoice));
            Assert.That(state.Cash.Cents, Is.EqualTo(5000));
            Assert.That(state.CustomersProcessed, Is.Zero);
        }

        [Test]
        public void SessionPublishesEventsOnlyForSuccessfulCommands()
        {
            var sink = new InMemoryShopEventSink();
            var session = new FirstSliceSession(CreateState(1000, 2), new SeededShopperPolicy(5), sink);
            Assert.That(session.LeaveForWholesaler().IsSuccess, Is.True);
            Assert.That(session.ArriveAtWholesaler().IsSuccess, Is.True);
            Assert.That(session.BuyAtWholesaler(new[] { 50, 0 }).IsSuccess, Is.False);
            Assert.That(session.BuyAtWholesaler(new[] { 2, 0 }).IsSuccess, Is.True);

            Assert.That(sink.Events.Count, Is.EqualTo(3));
            ShopEvent purchase = sink.Events[2];
            Assert.That(purchase.Kind, Is.EqualTo(ShopEventKind.CargoPurchased));
            Assert.That(purchase.Amount.Cents, Is.EqualTo(400));
            Assert.That(purchase.Quantities[0], Is.EqualTo(2));
        }

        [Test]
        public void SameSeedAndCommandsProduceSameDailyReport()
        {
            DailyReport first = RunDay(7231);
            DailyReport second = RunDay(7231);

            Assert.That(second.ClosingCash.Cents, Is.EqualTo(first.ClosingCash.Cents));
            Assert.That(second.SalesRevenue.Cents, Is.EqualTo(first.SalesRevenue.Cents));
            Assert.That(second.UnitsSold, Is.EqualTo(first.UnitsSold));
            Assert.That(first.UnitsLost, Is.EqualTo(1));
            Assert.That(
                first.OpeningCash.Cents - first.SupplierPurchases.Cents + first.SalesRevenue.Cents,
                Is.EqualTo(first.ClosingCash.Cents));
            Assert.That(first.NetProfit.Cents, Is.EqualTo(first.GrossProfit.Cents - first.LostGoods.Cents));
        }

        [Test]
        public void CanDriveBackToTheWholesalerToBuyMore()
        {
            ShopState state = AtWholesaler(CreateState(5000, 2));
            Assert.That(ShopRules.BuyAtWholesaler(state, new[] { 1, 0 }).IsSuccess, Is.True);
            Assert.That(ShopRules.LeaveWholesaler(state).IsSuccess, Is.True);
            Assert.That(ShopRules.ArriveAtWholesaler(state).IsSuccess, Is.True);
            Assert.That(ShopRules.BuyAtWholesaler(state, new[] { 1, 0 }).IsSuccess, Is.True);
            Assert.That(state.CargoAt(0), Is.EqualTo(2));
        }

        [Test]
        public void NextDayKeepsStockAndCashAndRestocksTheWholesaler()
        {
            ShopState state = AtWholesaler(CreateState(10000, 1));
            Assert.That(ShopRules.BuyAtWholesaler(state, new[] { 5, 0 }).IsSuccess, Is.True);
            DriveToStore(state);
            Assert.That(ShopRules.UnloadCargo(state, new[] { 5, 0 }).IsSuccess, Is.True);
            Assert.That(ShopRules.OpenStore(state).IsSuccess, Is.True);
            Assert.That(new FirstSliceSession(state, new SeededShopperPolicy(3)).ServeNextCustomer().IsSuccess, Is.True);
            Assert.That(ShopRules.CloseStore(state).IsSuccess, Is.True);
            long cash = state.Cash.Cents;
            int stock = state.InventoryAt(0);

            Assert.That(ShopRules.StartNextDay(state).IsSuccess, Is.True);
            Assert.That(state.Day, Is.EqualTo(2));
            Assert.That(state.Phase, Is.EqualTo(ShopPhase.AtStore));
            Assert.That(state.OpeningCash.Cents, Is.EqualTo(cash));
            Assert.That(state.InventoryAt(0), Is.EqualTo(stock));
            Assert.That(state.CustomersRemaining, Is.EqualTo(1));
            Assert.That(state.SupplierPurchases.Cents, Is.Zero);
            Assert.That(state.WholesalerStockAt(0), Is.EqualTo(20));
            Assert.That(ShopRules.StartNextDay(state).Code, Is.EqualTo(OperationCode.WrongPhase));
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
            return new ProductDefinition(id, "product." + id, cost, price, competitor, willingness,
                demandWeight, initialOrderQuantity: 2, maximumOrderQuantity: 10, wholesalerStock: 20);
        }

        private static ShopState AtWholesaler(ShopState state)
        {
            Assert.That(ShopRules.LeaveForWholesaler(state).IsSuccess, Is.True);
            Assert.That(ShopRules.ArriveAtWholesaler(state).IsSuccess, Is.True);
            return state;
        }

        private static void DriveToStore(ShopState state)
        {
            Assert.That(ShopRules.LeaveWholesaler(state).IsSuccess, Is.True);
            Assert.That(ShopRules.ArriveAtStore(state).IsSuccess, Is.True);
        }

        private static DailyReport RunDay(uint seed)
        {
            ShopState state = CreateState(10000, 8);
            var session = new FirstSliceSession(state, new SeededShopperPolicy(seed));
            Assert.That(session.LeaveForWholesaler().IsSuccess, Is.True);
            Assert.That(session.ArriveAtWholesaler().IsSuccess, Is.True);
            Assert.That(session.BuyAtWholesaler(new[] { 2, 2 }).IsSuccess, Is.True);
            Assert.That(session.LeaveWholesaler().IsSuccess, Is.True);
            Assert.That(session.ArriveAtStore().IsSuccess, Is.True);
            Assert.That(session.UnloadCargo(new[] { 2, 1 }).IsSuccess, Is.True);
            Assert.That(session.OpenStore().IsSuccess, Is.True);

            for (int i = 0; i < state.DailyCustomerCount; i++)
                Assert.That(session.ServeNextCustomer().IsSuccess, Is.True);

            Assert.That(session.CloseStore().IsSuccess, Is.True);
            return session.CreateReport();
        }
    }
}
