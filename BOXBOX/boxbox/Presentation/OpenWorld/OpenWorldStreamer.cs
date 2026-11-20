using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class OpenWorldStreamer : Node3D
{
    private const int CellSize = 4;
    private const int LoadRadius = 7;
    // Covers a complete nearby block so its buildings and props do not vanish into bare LOD tiles.
    private const int DetailRadius = 6;
    private const int CellsPerFrame = 3;
    private const float LotSurfaceY = 0.15f;
    private const float BuildingFootprint = 3.15f;
    private readonly Dictionary<Vector2I, LoadedCell> _loaded = [];
    private readonly List<PendingCell> _pending = [];
    private readonly HashSet<Vector2I> _required = [];
    private Vector2I _lastCenter = new(int.MaxValue, int.MaxValue);
    public PlayerController? Player { get; set; }

    public override void _Ready() => ThreadedSceneCache.Request(WorldAssetCatalog.Current.AllPaths);

    public override void _ExitTree()
    {
        _pending.Clear();
        _required.Clear();
        _loaded.Clear();
        ThreadedSceneCache.Clear();
    }

    public override void _Process(double delta)
    {
        if (Player is null) return;
        ThreadedSceneCache.Poll();
        if (!ThreadedSceneCache.IsReady) return;
        var focus = Player.FocusPosition;
        var center = new Vector2I(Mathf.FloorToInt(focus.X / CellSize), Mathf.FloorToInt(focus.Z / CellSize));
        if (center != _lastCenter)
        {
            _lastCenter = center;
            Refresh(center);
        }
        BuildPendingCells();
    }

    private void Refresh(Vector2I center)
    {
        _required.Clear();
        for (var x = center.X - LoadRadius; x <= center.X + LoadRadius; x++)
        for (var z = center.Y - LoadRadius; z <= center.Y + LoadRadius; z++)
        {
            var offset = new Vector2I(x - center.X, z - center.Y);
            var distanceSquared = offset.LengthSquared();
            if (distanceSquared > LoadRadius * LoadRadius) continue;
            var coordinate = new Vector2I(x, z);
            var detailed = distanceSquared <= DetailRadius * DetailRadius;
            _required.Add(coordinate);
            if (_loaded.TryGetValue(coordinate, out var loaded) && loaded.Detailed == detailed) continue;
            QueueCell(coordinate, detailed, distanceSquared);
        }

        foreach (var coordinate in _loaded.Keys.Where(item => !_required.Contains(item)).ToArray())
        {
            _loaded[coordinate].Node.QueueFree();
            _loaded.Remove(coordinate);
        }
        _pending.RemoveAll(item => !_required.Contains(item.Coordinate));
    }

    private void QueueCell(Vector2I coordinate, bool detailed, long distanceSquared)
    {
        _pending.RemoveAll(item => item.Coordinate == coordinate);
        _pending.Add(new PendingCell(coordinate, detailed, distanceSquared));
        _pending.Sort((left, right) => left.DistanceSquared.CompareTo(right.DistanceSquared));
    }

    private void BuildPendingCells()
    {
        for (var count = 0; count < CellsPerFrame && _pending.Count > 0; count++)
        {
            var pending = _pending[0];
            _pending.RemoveAt(0);
            if (!_required.Contains(pending.Coordinate)) continue;
            var node = BuildCell(pending.Coordinate, pending.Detailed);
            if (_loaded.TryGetValue(pending.Coordinate, out var previous)) previous.Node.QueueFree();
            _loaded[pending.Coordinate] = new LoadedCell(node, pending.Detailed);
            AddChild(node);
        }
    }

    private static Node3D BuildCell(Vector2I coordinate, bool detailed)
    {
        var cell = new Node3D
        {
            Name = $"Cell_{coordinate.X}_{coordinate.Y}",
            Position = new Vector3(coordinate.X * CellSize, 0, coordinate.Y * CellSize)
        };
        var plan = WorldLayoutRules.Plan(coordinate);
        if (plan.IsRoad)
        {
            BuildRoad(cell, coordinate, plan, detailed);
            return cell;
        }

        if (plan.IsRamp)
        {
            // Access cells now use the same low, continuous sidewalk as every lot.
            // The former wedge left an invisible side wall that trapped characters and cars.
            cell.AddChild(InstantiateAsset(Asset(WorldAssetCategory.RoadModules, "tile-low"),
                "ContinuousLowSidewalk", Vector3.Zero, 4f));
            cell.AddChild(CreateLowLotCollider());
            return cell;
        }

        cell.AddChild(InstantiateAsset(Asset(WorldAssetCategory.RoadModules, "tile-low"),
            "LowLotSurface", Vector3.Zero, 4f));
        // Keep walkable geometry solid across every loaded LOD. Detail controls only
        // buildings and decoration; it must never change traversal safety.
        cell.AddChild(CreateLowLotCollider());
        if (!detailed) return cell;

        if (plan.HasBuilding)
        {
            var sizeVariation = 0.82f + (plan.DetailSeed % 16) / 100f;
            var placement = InstantiateBuildingFitted(
                WorldAssetCatalog.Current.PickBuilding(plan.District, plan.DetailSeed),
                plan.FacingYaw, LotSurfaceY, sizeVariation);
            cell.AddChild(placement.Visual);
            if (plan.DetailSeed % 10 < 6)
                placement.Visual.AddChild(new NightWindowController { BuildingSeed = plan.DetailSeed });
            cell.AddChild(CreateBuildingCollider(placement));
        }
        else
        {
            BuildOpenLot(cell, plan);
        }
        UrbanLotDecorator.Decorate(cell, coordinate, plan, LotSurfaceY);
        return cell;
    }

    private static void BuildRoad(Node3D cell, Vector2I coordinate, WorldCellPlan plan, bool detailed)
    {
        var localX = FloorMod(coordinate.X, WorldLayoutRules.RoadPeriod);
        var localZ = FloorMod(coordinate.Y, WorldLayoutRules.RoadPeriod);
        var verticalRoad = localX < WorldLayoutRules.RoadWidthCells;
        var horizontalRoad = localZ < WorldLayoutRules.RoadWidthCells;
        if (verticalRoad && horizontalRoad && (localX != 0 || localZ != 0)) return;
        if (verticalRoad && !horizontalRoad && localX != 0) return;
        if (horizontalRoad && !verticalRoad && localZ != 0) return;

        var roadPath = Asset(WorldAssetCategory.RoadModules, plan.IsIntersection
            ? "road-crossroad"
            : plan.IsCrosswalk ? "road-crossing" : "road-straight");
        var roadPosition = plan.IsIntersection
            ? new Vector3(2, 0, 2)
            : verticalRoad ? new Vector3(2, 0, 0) : new Vector3(0, 0, 2);
        // Kenney's straight road travels along local X; local Z is its width.
        var roadScale = plan.IsIntersection ? new Vector3(8, 4, 8) : new Vector3(4, 4, 8);
        var road = InstantiateAsset(
            roadPath, plan.IsIntersection ? "WideIntersection" : "WideRoad", roadPosition, roadScale);
        if (!plan.IsIntersection && verticalRoad) road.RotateY(Mathf.Pi / 2f);
        cell.AddChild(road);
        if (!plan.IsIntersection) return;

        // Traffic law stays available for nearby low-detail road cells.
        // Detail controls lamp meshes and decoration, not the right of way.
        var intersection = new IntersectionTrafficController
        {
            Name = "SynchronizedIntersection",
            Position = new Vector3(2, 0, 2)
        };
        cell.AddChild(intersection);
        if (!detailed) return;
        AddTrafficSignal(cell, intersection, TrafficAxis.NorthSouth,
            "TrafficLightNorthWest", new Vector3(-1.3f, 0, -1.3f), 0);
        AddTrafficSignal(cell, intersection, TrafficAxis.EastWest,
            "TrafficLightNorthEast", new Vector3(5.3f, 0, -1.3f), Mathf.Pi / 2f);
        AddTrafficSignal(cell, intersection, TrafficAxis.NorthSouth,
            "TrafficLightSouthEast", new Vector3(5.3f, 0, 5.3f), Mathf.Pi);
        AddTrafficSignal(cell, intersection, TrafficAxis.EastWest,
            "TrafficLightSouthWest", new Vector3(-1.3f, 0, 5.3f), -Mathf.Pi / 2f);

        UrbanLotDecorator.AddIlluminatedStreetLight(cell, "IntersectionLampNW",
            new Vector3(-1.55f, 0, -1.55f), 1f, 0);
        UrbanLotDecorator.AddIlluminatedStreetLight(cell, "IntersectionLampSE",
            new Vector3(5.55f, 0, 5.55f), 1f, Mathf.Pi);
    }

    private static void AddTrafficSignal(Node3D cell, IntersectionTrafficController intersection,
        TrafficAxis axis, string name, Vector3 position, float yaw)
    {
        var root = CreateStreetFurniture(Asset(WorldAssetCategory.StreetFurniture, "traffic-light"),
            name, position, 3.2f, yaw);
        cell.AddChild(root);
        var light = root.GetChildren().OfType<TrafficLightController>().First();
        intersection.RegisterLight(axis, light);
    }

    private static void BuildOpenLot(Node3D cell, WorldCellPlan plan)
    {
        var treeCount = plan.NatureCluster switch
        {
            > 0.8f => 3,
            > 0.58f => 2,
            > 0.36f => 1,
            _ => 0
        };
        for (var index = 0; index < treeCount; index++)
        {
            var treePosition = index switch
            {
                0 => new Vector3(-0.78f, LotSurfaceY, 0.45f),
                1 => new Vector3(0.78f, LotSurfaceY, -0.55f),
                _ => new Vector3(0.22f, LotSurfaceY, 0.9f)
            };
            var treePath = WorldAssetCatalog.Current.PickByStemContains(WorldAssetCategory.Nature,
                plan.DetailSeed + (uint)(index * 97), "tree");
            var tree = InstantiateAsset(treePath, $"TreeVisual{index}", treePosition, 1f);
            VisualShadowSupport.NormalizeHeight(tree, 1.05f + (plan.DetailSeed % 5) * .1f);
            tree.RotateY(((plan.DetailSeed + (uint)(index * 97)) % 360) * Mathf.Pi / 180f);
            cell.AddChild(tree);
            cell.AddChild(CreateTreeCollider(treePosition));
        }

        if (plan.HasCombatTarget)
        {
            cell.AddChild(new DamageableTarget { Name = "CombatTarget", Position = new Vector3(0.55f, LotSurfaceY, -0.4f) });
        }
        else if (treeCount < 2 && plan.DetailSeed % 2 == 0)
        {
            cell.AddChild(InstantiateAsset(Asset(WorldAssetCategory.Nature, "plant"), "PlantVisual",
                new Vector3(0.62f, LotSurfaceY, -0.45f), 1.05f));
        }

        if (treeCount == 0 && plan.DetailSeed % 11 == 0)
            UrbanLotDecorator.AddVacantLotDetails(cell, plan, LotSurfaceY);
    }

    private static BuildingPlacement InstantiateBuildingFitted(
        string path, float yaw, float surfaceY, float sizeVariation)
    {
        var visual = InstantiateAsset(path, "BuildingVisual", Vector3.Zero, 1f);
        var bounds = CalculateVisualBounds(visual);
        var scale = MathF.Min(MathF.Min(
            BuildingFootprint * sizeVariation / MathF.Max(bounds.Size.X, 0.01f),
            BuildingFootprint * sizeVariation / MathF.Max(bounds.Size.Z, 0.01f)), 3.25f);
        var rotatedCenter = new Basis(Vector3.Up, yaw) *
            new Vector3(bounds.Center.X * scale, 0, bounds.Center.Z * scale);
        visual.Scale = Vector3.One * scale;
        visual.Rotation = new Vector3(0, yaw, 0);
        visual.Position = new Vector3(-rotatedCenter.X, surfaceY - bounds.Min.Y * scale, -rotatedCenter.Z);
        return new BuildingPlacement(visual, bounds, scale, yaw, surfaceY);
    }

    private static VisualBounds CalculateVisualBounds(Node3D root)
    {
        var hasPoint = false;
        var minimum = Vector3.Zero;
        var maximum = Vector3.Zero;
        AccumulateBounds(root, Transform3D.Identity, ref hasPoint, ref minimum, ref maximum);
        return hasPoint ? new(minimum, maximum) : new(new(-0.5f, 0, -0.5f), new(0.5f, 1, 0.5f));
    }

    private static void AccumulateBounds(Node3D node, Transform3D parentTransform,
        ref bool hasPoint, ref Vector3 minimum, ref Vector3 maximum)
    {
        var transform = parentTransform * node.Transform;
        if (node is MeshInstance3D { Mesh: not null } meshInstance)
        {
            var box = meshInstance.Mesh.GetAabb();
            for (var index = 0; index < 8; index++)
            {
                var point = transform * box.GetEndpoint(index);
                if (!hasPoint) { minimum = point; maximum = point; hasPoint = true; }
                else { minimum = minimum.Min(point); maximum = maximum.Max(point); }
            }
        }
        foreach (var child in node.GetChildren().OfType<Node3D>())
            AccumulateBounds(child, transform, ref hasPoint, ref minimum, ref maximum);
    }

    private static Node3D InstantiateAsset(string path, string name, Vector3 position, float scale)
        => InstantiateAsset(path, name, position, Vector3.One * scale);

    private static Node3D InstantiateAsset(string path, string name, Vector3 position, Vector3 scale)
    {
        var scene = ThreadedSceneCache.Get(path);
        var instance = scene.Instantiate<Node3D>();
        instance.Name = name;
        instance.Position = position;
        instance.Scale = scale;
        VisualShadowSupport.SetCastShadows(instance);
        return instance;
    }

    private static Node3D CreateStreetFurniture(string path, string name, Vector3 position, float scale, float yaw)
    {
        var root = new Node3D { Name = name, Position = position, Rotation = new(0, yaw, 0) };
        var visual = InstantiateAsset(path, $"{name}Visual", Vector3.Zero, scale);
        root.AddChild(visual);
        if (name.StartsWith("TrafficLight", StringComparison.Ordinal))
        {
            var controller = new TrafficLightController { Name = $"{name}Cycle" };
            root.AddChild(controller);
            controller.Bind(visual);
        }
        var body = new StaticBody3D { Name = $"{name}Collision" };
        var trafficLight = name.StartsWith("TrafficLight", StringComparison.Ordinal);
        body.AddChild(new CollisionShape3D
        {
            Position = trafficLight ? new Vector3(0, .82f, 0) : new Vector3(0, 0.72f * scale, 0),
            Shape = trafficLight
                ? new CylinderShape3D { Radius = .1f, Height = 1.65f }
                : new CylinderShape3D { Radius = 0.07f * scale, Height = 1.44f * scale }
        });
        root.AddChild(body);
        return root;
    }

    private static StaticBody3D CreateBuildingCollider(BuildingPlacement placement)
    {
        var size = placement.Bounds.Size * placement.Scale;
        var body = new StaticBody3D { Name = "InvisibleBuildingCollision" };
        body.AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, placement.SurfaceY + size.Y / 2f, 0),
            Rotation = new Vector3(0, placement.Yaw, 0),
            Shape = new BoxShape3D { Size = new Vector3(size.X * 0.92f, size.Y, size.Z * 0.92f) }
        });
        return body;
    }

    private static StaticBody3D CreateLowLotCollider()
    {
        var body = new StaticBody3D { Name = "ExactLowLotCollision" };
        body.AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, LotSurfaceY / 2f, 0),
            Shape = new BoxShape3D { Size = new Vector3(4f, LotSurfaceY, 4f) }
        });
        return body;
    }

    private static StaticBody3D CreateTreeCollider(Vector3 basePosition)
    {
        var body = new StaticBody3D { Name = "InvisibleTreeCollision" };
        body.AddChild(new CollisionShape3D
        {
            Position = basePosition + Vector3.Up * 0.65f,
            Shape = new CylinderShape3D { Radius = 0.3f, Height = 1.3f }
        });
        return body;
    }

    private static int FloorMod(int value, int divisor) => ((value % divisor) + divisor) % divisor;
    private static string Asset(WorldAssetCategory category, string stem) =>
        WorldAssetCatalog.Current.RequireByStem(category, stem);
    private readonly record struct VisualBounds(Vector3 Min, Vector3 Max)
    {
        public Vector3 Size => Max - Min;
        public Vector3 Center => (Min + Max) / 2f;
    }
    private readonly record struct BuildingPlacement(Node3D Visual, VisualBounds Bounds, float Scale, float Yaw, float SurfaceY);
    private readonly record struct LoadedCell(Node3D Node, bool Detailed);
    private readonly record struct PendingCell(Vector2I Coordinate, bool Detailed, long DistanceSquared);
}
