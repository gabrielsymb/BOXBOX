using System;
using System.Collections.Generic;
using BoxBox.Domain;

namespace BoxBox.Application
{
    /// <summary>
    /// Use cases of the first playable day. Each command runs the pure ShopRules and, only on
    /// success, publishes what changed to the event sink.
    /// </summary>
    public sealed class FirstSliceSession
    {
        private readonly IShopperPolicy _shopperPolicy;
        private readonly IShopEventSink _events;

        public ShopState State { get; }
        public CustomerDecision? LastCustomerDecision { get; private set; }

        public FirstSliceSession(ShopState state, IShopperPolicy shopperPolicy, IShopEventSink events = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _shopperPolicy = shopperPolicy ?? throw new ArgumentNullException(nameof(shopperPolicy));
            _events = events ?? new NullShopEventSink();
        }

        public OperationResult LeaveForWholesaler() => PhaseCommand(ShopRules.LeaveForWholesaler(State));
        public OperationResult ArriveAtWholesaler() => PhaseCommand(ShopRules.ArriveAtWholesaler(State));
        public OperationResult LeaveWholesaler() => PhaseCommand(ShopRules.LeaveWholesaler(State));
        public OperationResult ArriveAtStore() => PhaseCommand(ShopRules.ArriveAtStore(State));
        public OperationResult OpenStore() => PhaseCommand(ShopRules.OpenStore(State));
        public OperationResult CloseStore() => PhaseCommand(ShopRules.CloseStore(State));

        public OperationResult BuyAtWholesaler(IReadOnlyList<int> quantities)
        {
            Money cashBefore = State.Cash;
            OperationResult result = ShopRules.BuyAtWholesaler(State, quantities);
            if (result.IsSuccess)
                _events.Publish(new ShopEvent(ShopEventKind.CargoPurchased, State.Phase,
                    Copy(quantities), cashBefore - State.Cash));
            return result;
        }

        public OperationResult UnloadCargo(IReadOnlyList<int> delivered)
        {
            Money lostBefore = State.LostGoods;
            int unitsLostBefore = State.UnitsLost;
            OperationResult result = ShopRules.UnloadCargo(State, delivered);
            if (result.IsSuccess)
                _events.Publish(new ShopEvent(ShopEventKind.CargoUnloaded, State.Phase, Copy(delivered),
                    State.LostGoods - lostBefore, State.UnitsLost - unitsLostBefore));
            return result;
        }

        public OperationResult SetRetailPrice(string productId, Money price)
        {
            return ShopRules.SetRetailPrice(State, productId, price);
        }

        public OperationResult ServeNextCustomer()
        {
            if (State.Phase != ShopPhase.Open)
                return OperationResult.Failure(OperationCode.WrongPhase);
            if (State.CustomersRemaining <= 0)
                return OperationResult.Failure(OperationCode.NoCustomersRemaining);

            Money revenueBefore = State.SalesRevenue;
            CustomerDecision decision = _shopperPolicy.Decide(State.CreateSnapshot());
            OperationResult result = ShopRules.ServeCustomer(State, decision);
            if (result.IsSuccess)
            {
                LastCustomerDecision = decision;
                _events.Publish(new ShopEvent(ShopEventKind.CustomerServed, State.Phase,
                    amount: State.SalesRevenue - revenueBefore, customer: decision));
            }
            return result;
        }

        public DailyReport CreateReport()
        {
            return ShopRules.CreateReport(State);
        }

        private OperationResult PhaseCommand(OperationResult result)
        {
            if (result.IsSuccess)
                _events.Publish(new ShopEvent(ShopEventKind.PhaseChanged, State.Phase));
            return result;
        }

        private static int[] Copy(IReadOnlyList<int> values)
        {
            var copy = new int[values.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = values[i];
            return copy;
        }
    }
}
