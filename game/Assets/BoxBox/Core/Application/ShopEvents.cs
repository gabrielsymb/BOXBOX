using System.Collections.Generic;
using BoxBox.Domain;

namespace BoxBox.Application
{
    // Event sink pattern adapted from Moonforge RPG Engine (MIT, 3583Bytes):
    // unity-packages/com.moonforge.core/Runtime/Runtime/Events/IDomainEventSink.cs and
    // Shops/Events/ShopTransactionEvent.cs. Events are published only after a command
    // succeeded, so listeners (HUD, audio, the world) never see a half-applied change.

    public enum ShopEventKind
    {
        PhaseChanged,
        CargoPurchased,
        CargoUnloaded,
        CustomerServed
    }

    public sealed class ShopEvent
    {
        public ShopEventKind Kind { get; }
        public ShopPhase Phase { get; }
        /// <summary>Units per product index: bought, or delivered on unload. Empty otherwise.</summary>
        public IReadOnlyList<int> Quantities { get; }
        /// <summary>Money paid on purchase, lost cargo cost on unload, sale price for a customer.</summary>
        public Money Amount { get; }
        public int UnitsLost { get; }
        public CustomerDecision? Customer { get; }

        public ShopEvent(
            ShopEventKind kind,
            ShopPhase phase,
            IReadOnlyList<int> quantities = null,
            Money amount = default,
            int unitsLost = 0,
            CustomerDecision? customer = null)
        {
            Kind = kind;
            Phase = phase;
            Quantities = quantities ?? new int[0];
            Amount = amount;
            UnitsLost = unitsLost;
            Customer = customer;
        }
    }

    public interface IShopEventSink
    {
        void Publish(ShopEvent shopEvent);
    }

    public sealed class InMemoryShopEventSink : IShopEventSink
    {
        private readonly List<ShopEvent> _events = new List<ShopEvent>();

        public IReadOnlyList<ShopEvent> Events => _events;

        public void Publish(ShopEvent shopEvent) => _events.Add(shopEvent);
    }

    internal sealed class NullShopEventSink : IShopEventSink
    {
        public void Publish(ShopEvent shopEvent) { }
    }
}
