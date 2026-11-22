namespace Boxbox.Application.BusinessDay;

public enum GrocerySupplyStage
{
    NeedOrder,
    Reserved,
    Loaded,
    Delivered,
    Trading
}

/// <summary>Ownership of the confirmed grocery lot as it moves between the two stores.</summary>
public sealed class GrocerySupplyRun
{
    public GroceryOrderSession Order { get; } = new();
    public GrocerySupplyStage Stage { get; private set; }
    public int UnitsOnPickup => Stage == GrocerySupplyStage.Loaded ? Order.ConfirmedUnitCount : 0;

    public bool RecordReservation()
    {
        if (Stage != GrocerySupplyStage.NeedOrder || Order.ConfirmedUnitCount == 0)
        {
            return false;
        }

        Stage = GrocerySupplyStage.Reserved;
        return true;
    }

    public bool TryLoad(bool pickupParkedAtSupplier)
    {
        if (Stage != GrocerySupplyStage.Reserved || !pickupParkedAtSupplier)
        {
            return false;
        }

        Stage = GrocerySupplyStage.Loaded;
        return true;
    }

    public bool TryUnload(bool pickupParkedAtGrocery)
    {
        if (Stage != GrocerySupplyStage.Loaded || !pickupParkedAtGrocery)
        {
            return false;
        }

        Stage = GrocerySupplyStage.Delivered;
        return true;
    }

    public bool TryStartTrading()
    {
        if (Stage != GrocerySupplyStage.Delivered)
        {
            return false;
        }

        Stage = GrocerySupplyStage.Trading;
        return true;
    }

    public void Replay()
    {
        Order.ResetConfirmation();
        Stage = GrocerySupplyStage.NeedOrder;
    }
}
