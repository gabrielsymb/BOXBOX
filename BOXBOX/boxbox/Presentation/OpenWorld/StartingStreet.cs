using Godot;

namespace Boxbox.Presentation.OpenWorld;

/// <summary>Fixed places in the starting corridor, independent of streamed decoration.</summary>
internal static class StartingStreet
{
    private const int GroceryCell = 3;
    private const int SupplierCell = 19;
    private const float CellSize = 4f;

    public static Vector3 PlayerStart => new(GroceryCell * CellSize + 5.65f, 0.22f, 7.8f);
    public static Vector3 PickupParking => new(GroceryCell * CellSize + 2f, 0.15f, 4.05f);

    public static bool IsAuthoredShopBlock(Vector2I cell) =>
        cell.X is >= 2 and <= 7 && cell.Y is >= 2 and <= 7;

    public static bool ReservesSupplierLot(Vector2I cell) =>
        cell.Y is 2 or 3 && cell.X is SupplierCell or SupplierCell + 1;

    public static (StreetStore Grocery, StreetStore Supplier) PlaceStores(Node3D world)
    {
        world.AddChild(GD.Load<PackedScene>("res://Presentation/OpenWorld/ShopBlock.tscn").Instantiate());
        return (PlaceStore(world, GroceryCell, "GroceryInterior", "res://Assets/World/StartingBlock/mercearia-fachada.glb", "BOXBOX"),
            PlaceStore(world, SupplierCell, "SupplierInterior", "res://Assets/World/Buildings/Commercial/corner-shop-awning.glb", "ATACADO"));
    }

    private static StreetStore PlaceStore(Node3D world, int cell, string interiorName,
        string facadePath, string sign)
    {
        var center = new Vector3(cell * CellSize + 2, 0.155f, 10.5f);
        var facade = new Node3D
        {
            Name = interiorName.Replace("Interior", "Exterior"),
            Position = center,
            Rotation = new Vector3(0, Mathf.Pi, 0)
        };
        world.AddChild(facade);

        var isGrocery = cell == GroceryCell;
        var building = GD.Load<PackedScene>(facadePath).Instantiate<Node3D>();
        building.Scale = isGrocery ? new Vector3(7.1f, 3.85f, 4.15f) : Vector3.One * 3.1f;
        building.Rotation = isGrocery ? new Vector3(0, Mathf.Pi, 0) : Vector3.Zero;
        facade.AddChild(building);
        VisualShadowSupport.SetCastShadows(building);

        var exteriorCollision = new StaticBody3D();
        exteriorCollision.AddChild(new CollisionShape3D
        {
            Position = isGrocery ? new Vector3(0, 2.30f, 0) : new Vector3(0, 1.39f, 0),
            Shape = new BoxShape3D
            {
                Size = isGrocery ? new Vector3(6.18f, 4.60f, 3.85f) : new Vector3(2.74f, 2.78f, 2.78f)
            }
        });
        facade.AddChild(exteriorCollision);
        facade.AddChild(new Label3D
        {
            Name = "StreetSign",
            Text = sign,
            Position = isGrocery ? new Vector3(0, 2.53f, 1.99f) : new Vector3(0, 2.15f, 2.47f),
            PixelSize = isGrocery ? 0.016f : 0.0055f,
            FontSize = 43,
            OutlineSize = 8,
            Modulate = new Color("#fff0cb"),
            Billboard = isGrocery ? BaseMaterial3D.BillboardModeEnum.Disabled : BaseMaterial3D.BillboardModeEnum.Enabled
        });

        facade.AddChild(new Label3D
        {
            Name = "RearStoreSign",
            Text = sign,
            Position = isGrocery ? new Vector3(0, 3.78f, -2.27f) : new Vector3(0, 2.88f, -1.50f),
            Rotation = new Vector3(0, Mathf.Pi, 0),
            PixelSize = isGrocery ? 0.014f : 0.009f,
            FontSize = 48,
            OutlineSize = 10,
            Modulate = new Color("#fff0cb")
        });

        var interior = GD.Load<PackedScene>($"res://Presentation/Grocery/{interiorName}.tscn").Instantiate<Node3D>();
        // Keep the wider floors apart from the physical street and its neighbors.
        interior.Position = new Vector3(center.X, 5f, -160f);
        interior.Rotation = facade.Rotation;
        world.AddChild(interior);
        InteriorRenderLayer.Include(interior);
        return new StreetStore(facade, exteriorCollision, interior, center);
    }
}

internal sealed class StreetStore(
    Node3D facade,
    StaticBody3D facadeCollision,
    Node3D interior,
    Vector3 center)
{
    public Vector3 Door => center + new Vector3(0, 0, -2.95f);
    private Vector3 FloorSize => ((BoxShape3D)interior
        .GetNode<CollisionShape3D>("FloorCollision/Bounds").Shape).Size;

    public Vector3 JustInside => interior.GetNode<Marker3D>("EntryPoint").GlobalPosition;
    public Vector3 Parking => center + new Vector3(0, 0, -5.78f);
    public Vector3 Counter => interior.GetNode<Marker3D>("ServicePoint").GlobalPosition;
    public Vector3 InteriorFocus => interior.GlobalPosition + Vector3.Up * 0.72f;
    public float InteriorCameraSize => Mathf.Max(FloorSize.X, FloorSize.Z) * 1.16f;

    public Vector3 ClampInteriorPosition(Vector3 position)
    {
        var local = interior.ToLocal(position);
        var bounds = FloorSize * 0.5f - new Vector3(0.24f, 0, 0.24f);
        local.X = Mathf.Clamp(local.X, -bounds.X, bounds.X);
        local.Z = Mathf.Clamp(local.Z, -bounds.Z, bounds.Z);
        return interior.ToGlobal(local);
    }

    public void SetInterior(bool entered)
    {
        facade.Visible = !entered;
        facadeCollision.CollisionLayer = entered ? 0u : 1u;
        interior.Visible = entered;
        foreach (var body in interior.FindChildren("*", "StaticBody3D", true, false).OfType<StaticBody3D>())
            body.CollisionLayer = entered ? 1u : 0u;
    }
}
