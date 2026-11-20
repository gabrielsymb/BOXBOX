using Godot;

namespace Boxbox.Presentation.OpenWorld;

/// <summary>
/// Adds deterministic, presentation-only detail to streamed urban lots.
/// The economic simulation never reads these nodes.
/// </summary>
public static class UrbanLotDecorator
{
    private static StandardMaterial3D? _grassMaterial;
    public static void Decorate(Node3D cell, Vector2I coordinate, WorldCellPlan plan, float surfaceY)
    {
        var localX = FloorMod(coordinate.X, WorldLayoutRules.RoadPeriod);
        var localZ = FloorMod(coordinate.Y, WorldLayoutRules.RoadPeriod);
        var edge = localX == WorldLayoutRules.RoadWidthCells ||
                   localX == WorldLayoutRules.RoadPeriod - 1 ||
                   localZ == WorldLayoutRules.RoadWidthCells ||
                   localZ == WorldLayoutRules.RoadPeriod - 1;

        switch (plan.BlockTemplate)
        {
            case UrbanBlockTemplate.ResidentialCluster:
            case UrbanBlockTemplate.NeighborhoodGate:
                AddResidentialDetails(cell, plan, surfaceY, edge);
                break;
            case UrbanBlockTemplate.CommercialStrip:
            case UrbanBlockTemplate.MixedCourtyard:
                AddCommercialDetails(cell, plan, surfaceY, edge);
                break;
            case UrbanBlockTemplate.PocketPark:
                AddParkDetails(cell, plan, surfaceY, edge);
                break;
            case UrbanBlockTemplate.MarketSquare:
                AddSquareDetails(cell, plan, surfaceY, edge);
                break;
        }
    }

    private static void AddResidentialDetails(Node3D cell, WorldCellPlan plan, float y, bool edge)
    {
        AddGrassSurface(cell, y);
        AddGrassCluster(cell, plan.DetailSeed, y + 0.025f, plan.HasBuilding ? 1 : 2);
        if (edge && plan.DetailSeed % 3 != 0)
        {
            AddAsset(cell, LotProp("fence-low"), "YardFence", new Vector3(0, y, 1.5f), 0.72f,
                plan.FacingYaw + Mathf.Pi / 2f);
        }

        if (!plan.HasBuilding && plan.DetailSeed % 4 == 0)
        {
            AddAsset(cell, LotProp("trashcan"), "ResidentialTrashCan", new Vector3(1.15f, y, -1.2f), 0.72f,
                plan.FacingYaw);
        }
    }

    private static void AddCommercialDetails(Node3D cell, WorldCellPlan plan, float y, bool edge)
    {
        if (edge && plan.DetailSeed % 3 == 0)
        {
            AddIlluminatedStreetLight(cell, "SidewalkLight", new Vector3(1.35f, y, 1.35f), 1f,
                plan.FacingYaw);
        }

        if (!plan.HasBuilding && plan.DetailSeed % 4 == 0)
        {
            AddAsset(cell, LotProp("dumpster"), "ServiceDumpster", new Vector3(-0.7f, y, 0.55f), 0.78f,
                plan.FacingYaw);
            AddAsset(cell, LotProp("construction-cone"), "ServiceCone", new Vector3(0.72f, y, -0.55f), 0.68f, 0);
        }
        else if (!plan.HasBuilding)
        {
            AddGrassCluster(cell, plan.DetailSeed, y, 2);
        }
    }

    private static void AddParkDetails(Node3D cell, WorldCellPlan plan, float y, bool edge)
    {
        AddGrassSurface(cell, y);
        AddGrassCluster(cell, plan.DetailSeed, y + 0.025f, 2 + (int)(plan.DetailSeed % 2));
        if (plan.DetailSeed % 4 == 0)
        {
            AddAsset(cell, PickBench(plan.DetailSeed), "ParkBench", new Vector3(0.65f, y, -0.35f), 1f,
                plan.FacingYaw);
        }
        if (edge && plan.DetailSeed % 5 == 0)
        {
            AddAsset(cell, LotProp("trashcan"), "ParkTrashCan", new Vector3(-1.25f, y, 1.1f), 0.68f, 0);
        }
    }

    private static void AddSquareDetails(Node3D cell, WorldCellPlan plan, float y, bool edge)
    {
        if (edge && plan.DetailSeed % 2 == 0)
        {
            AddIlluminatedStreetLight(cell, "SquareLight", new Vector3(1.3f, y, 1.3f), 1f,
                plan.FacingYaw);
        }
        if (!plan.HasBuilding && plan.DetailSeed % 3 == 0)
        {
            AddAsset(cell, PickBench(plan.DetailSeed + 31), "SquareBench", new Vector3(0, y, 0.45f), 1f,
                plan.FacingYaw);
            AddAsset(cell, LotProp("trashcan"), "SquareTrashCan", new Vector3(-1.05f, y, -0.7f), 0.65f, 0);
        }
    }

    public static void AddVacantLotDetails(Node3D cell, WorldCellPlan plan, float y)
    {
        AddGrassCluster(cell, plan.DetailSeed, y, 2);
        AddAsset(cell, LotProp("construction-fence"), "VacantLotFence", new Vector3(0, y, 1.3f), 0.8f,
            plan.FacingYaw + Mathf.Pi / 2f);
        if (plan.DetailSeed % 2 == 0)
        {
            AddAsset(cell, LotProp("construction-cone"), "VacantLotCone", new Vector3(-0.75f, y, -0.6f), 0.72f, 0);
        }
    }

    private static void AddGrassSurface(Node3D cell, float y)
    {
        var surface = new MeshInstance3D
        {
            Name = "InsetGrassTerrain",
            Position = new Vector3(0, y + 0.018f, 0),
            Mesh = new BoxMesh { Size = new Vector3(3.28f, 0.035f, 3.28f), Material = GrassMaterial() },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        cell.AddChild(surface);
    }

    private static StandardMaterial3D GrassMaterial()
    {
        if (_grassMaterial is not null) return _grassMaterial;
        var noise = new FastNoiseLite { Frequency = 0.42f, FractalOctaves = 2 };
        var texture = new NoiseTexture2D
        {
            Width = 128,
            Height = 128,
            Seamless = true,
            Noise = noise,
            ColorRamp = new Gradient
            {
                Offsets = [0f, 0.5f, 1f],
                Colors = [Color.FromHtml("4f7844"), Color.FromHtml("5d874b"), Color.FromHtml("6a9256")]
            }
        };
        _grassMaterial = new StandardMaterial3D
        {
            ResourceName = "GrassTerrainNoise",
            AlbedoTexture = texture,
            Roughness = 0.96f
        };
        return _grassMaterial;
    }

    private static void AddGrassCluster(Node3D cell, uint seed, float y, int count)
    {
        ReadOnlySpan<Vector3> positions =
        [
            new(-1.05f, 0, -0.82f), new(0.92f, 0, 0.74f), new(-0.8f, 0, 0.92f),
            new(1.12f, 0, -0.88f), new(0.12f, 0, 1.16f), new(-1.2f, 0, 0.05f),
            new(0.42f, 0, -1.18f)
        ];
        for (var index = 0; index < Math.Min(count, positions.Length); index++)
        {
            var positionIndex = (index + (int)(seed % (uint)positions.Length)) % positions.Length;
            var yaw = ((seed + (uint)(index * 71)) % 360) * Mathf.Pi / 180f;
            var detailPath = WorldAssetCatalog.Current.PickByStemContains(WorldAssetCategory.Nature,
                seed + (uint)(index * 71), "plant", "stones", "rocks", "planter");
            AddAsset(cell, detailPath, $"NatureDetail{index}", positions[positionIndex] + Vector3.Up * y,
                1f, yaw);
        }
    }

    private static void AddAsset(Node3D parent, string path, string name, Vector3 position,
        float scale, float yaw)
    {
        var instance = ThreadedSceneCache.Get(path).Instantiate<Node3D>();
        instance.Name = name;
        instance.Position = position;
        instance.Scale = Vector3.One * scale;
        instance.Rotation = new Vector3(0, yaw, 0);
        var isBench = name.EndsWith("Bench", StringComparison.Ordinal);
        if (isBench) VisualShadowSupport.NormalizeHorizontalLength(instance, 1.25f);
        var stem = System.IO.Path.GetFileNameWithoutExtension(path);
        if (name.StartsWith("NatureDetail", StringComparison.Ordinal))
        {
            var target = stem.Contains("planter", StringComparison.OrdinalIgnoreCase) ? .48f :
                stem.Contains("plant", StringComparison.OrdinalIgnoreCase) ? .34f : .58f;
            VisualShadowSupport.NormalizeHorizontalLength(instance, target);
        }
        var isTinyGroundDetail = name.StartsWith("GrassPatch", StringComparison.Ordinal);
        VisualShadowSupport.SetCastShadows(instance, !isTinyGroundDetail);
        parent.AddChild(instance);
        if (isBench)
        {
            var body = new StaticBody3D
            {
                Name = $"{name}Collision",
                Position = position,
                Rotation = new Vector3(0, yaw, 0)
            };
            body.AddChild(new CollisionShape3D
            {
                Position = new Vector3(0, .22f, 0),
                Shape = new BoxShape3D { Size = new Vector3(1.2f, .44f, .42f) }
            });
            parent.AddChild(body);
        }
    }

    public static void AddIlluminatedStreetLight(Node3D parent, string name, Vector3 position,
        float scale, float yaw)
    {
        var root = new Node3D { Name = name, Position = position, Rotation = new Vector3(0, yaw, 0) };
        var visual = ThreadedSceneCache.Get(StreetFurniture("light-square")).Instantiate<Node3D>();
        visual.Name = $"{name}Visual";
        VisualShadowSupport.NormalizeHeight(visual, 1.55f * scale);
        root.AddChild(visual);
        VisualShadowSupport.SetCastShadows(visual);
        root.AddChild(new NightActivatedLight
        {
            Name = $"{name}WarmLight",
            Position = new Vector3(0, 1.48f * scale, 0)
        });
        var body = new StaticBody3D { Name = $"{name}Collision" };
        body.AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, .78f * scale, 0),
            Shape = new CylinderShape3D { Radius = .12f * scale, Height = 1.56f * scale }
        });
        root.AddChild(body);
        parent.AddChild(root);
    }

    private static int FloorMod(int value, int divisor) => ((value % divisor) + divisor) % divisor;
    private static string Nature(string stem) =>
        WorldAssetCatalog.Current.RequireByStem(WorldAssetCategory.Nature, stem);
    private static string LotProp(string stem) =>
        WorldAssetCatalog.Current.RequireByStem(WorldAssetCategory.LotProps, stem);
    private static string PickBench(uint seed) =>
        WorldAssetCatalog.Current.PickByStemContains(WorldAssetCategory.LotProps, seed, "bench");
    private static string StreetFurniture(string stem) =>
        WorldAssetCatalog.Current.RequireByStem(WorldAssetCategory.StreetFurniture, stem);
}
