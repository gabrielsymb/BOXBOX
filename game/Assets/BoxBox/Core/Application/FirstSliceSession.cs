using System.Collections.Generic;
using System;
using BoxBox.Domain;

namespace BoxBox.Application
{
    public sealed class FirstSliceSession
    {
        private readonly IShopperPolicy _shopperPolicy;

        public ShopState State { get; }
        public CustomerDecision? LastCustomerDecision { get; private set; }

        public FirstSliceSession(
            ShopState state,
            IShopperPolicy shopperPolicy)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _shopperPolicy = shopperPolicy ?? throw new ArgumentNullException(nameof(shopperPolicy));
        }

        public OperationResult PlaceOrder(IReadOnlyList<int> quantities)
        {
            return ShopRules.PlaceOrder(State, quantities);
        }

        public OperationResult ArriveAtWholesaler()
        {
            return ShopRules.ArriveAtWholesaler(State);
        }

        public OperationResult LoadCargo()
        {
            return ShopRules.LoadCargo(State);
        }

        public OperationResult ArriveAtStore()
        {
            return ShopRules.ArriveAtStore(State);
        }

        public OperationResult UnloadCargo()
        {
            return ShopRules.UnloadCargo(State);
        }

        public OperationResult OpenStore()
        {
            return ShopRules.OpenStore(State);
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

            CustomerDecision decision = _shopperPolicy.Decide(State.CreateSnapshot());
            OperationResult result = ShopRules.ServeCustomer(State, decision);
            if (result.IsSuccess)
                LastCustomerDecision = decision;
            return result;
        }

        public OperationResult CloseStore()
        {
            return ShopRules.CloseStore(State);
        }

        public DailyReport CreateReport()
        {
            return ShopRules.CreateReport(State);
        }
    }
}
