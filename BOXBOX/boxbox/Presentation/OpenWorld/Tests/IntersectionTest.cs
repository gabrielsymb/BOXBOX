using Godot;

namespace Boxbox.Presentation.OpenWorld.Tests;

/// <summary>
/// Isolated diagnostic fixture. The ground and camera are technical placeholders,
/// not presentation assets. It intentionally observes the current deadlock-prone
/// behavior without resolving it.
/// </summary>
public sealed partial class IntersectionTest : Node3D
{
    private const double TestDurationSeconds = 120.0;
    private static readonly Vector3 IntersectionCenter = new(2f, 0f, 2f);
    private readonly List<VehicleObservation> _observations = [];
    private double _elapsedSeconds;
    private int _deadlockCount;
    private bool _captureOverlay;
    private bool _captureSaved;

    public override void _Ready()
    {
        _captureOverlay = OS.GetCmdlineUserArgs().Any(argument =>
            argument.TrimStart('-').Equals("capture-overlay", StringComparison.Ordinal));
        CreateGround();
        CreateCamera();

        AddChild(new IntersectionTrafficController
        {
            Name = "TestIntersection",
            Position = IntersectionCenter
        });

        var visualPath = WorldAssetCatalog.Current.RequireByStem(WorldAssetCategory.Vehicles, "sedan");
        SpawnVehicle("Northbound", new Vector3(0.55f, 0.35f, -8f), Vector3.Back, 100u, visualPath);
        SpawnVehicle("Southbound", new Vector3(3.45f, 0.35f, 12f), Vector3.Forward, 200u, visualPath);
        SpawnVehicle("Eastbound", new Vector3(-8f, 0.35f, 3.45f), Vector3.Right, 300u, visualPath);
        SpawnVehicle("Westbound", new Vector3(12f, 0.35f, 0.55f), Vector3.Left, 400u, visualPath);

        GD.Print("INTERSECTION_TEST inicio duracao=120s veiculos=4 manobra=esquerda seed=100,200,300,400");
    }

    public override void _PhysicsProcess(double delta)
    {
        _elapsedSeconds += delta;
        ObserveCrossings();
        if (_captureOverlay && _captureSaved && _elapsedSeconds >= 2.0)
        {
            GetTree().Quit();
            return;
        }
        if (_elapsedSeconds < TestDurationSeconds) return;

        var crossedCount = _observations.Count(item => item.Crossed);
        GD.Print($"INTERSECTION_TEST resumo deadlocks={_deadlockCount} carros_atravessaram={crossedCount}/4 " +
                 $"duracao={_elapsedSeconds:F1}s");
        GetTree().Quit();
    }

    private void SpawnVehicle(string name, Vector3 position, Vector3 direction, uint seed, string visualPath)
    {
        var vehicle = new TrafficVehicle { Name = name, Position = position };
        vehicle.Initialize(direction, visualPath, seed);
        vehicle.DeadlockDetected += OnDeadlockDetected;
        AddChild(vehicle);
        vehicle.SetDebugOverlayVisible(_captureOverlay);
        _observations.Add(new VehicleObservation(vehicle));
    }

    private void OnDeadlockDetected(TrafficVehicle vehicle) => _deadlockCount++;

    private void ObserveCrossings()
    {
        foreach (var observation in _observations)
        {
            if (!IsInstanceValid(observation.Vehicle) || observation.Crossed) continue;
            var distance = observation.Vehicle.GlobalPosition.DistanceTo(IntersectionCenter);
            if (distance <= 4.25f) observation.Entered = true;
            if (observation.Entered && distance >= 7.0f) observation.Crossed = true;
        }
    }

    public override void _Process(double delta)
    {
        if (!_captureOverlay || _captureSaved || _elapsedSeconds < 1.0) return;
        _captureSaved = true;
        var path = ProjectSettings.GlobalizePath("user://intersection-debug-overlay.png");
        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"INTERSECTION_TEST captura={path} resultado={error}");
    }

    private void CreateGround()
    {
        var ground = new StaticBody3D
        {
            Name = "TestGround",
            CollisionLayer = OpenWorldCollisionLayers.Scenario,
            CollisionMask = OpenWorldCollisionLayers.AllActors
        };
        ground.AddChild(new CollisionShape3D
        {
            Position = new Vector3(2f, -0.28f, 2f),
            Shape = new BoxShape3D { Size = new Vector3(64f, 0.5f, 64f) }
        });
        AddChild(ground);
    }

    private void CreateCamera()
    {
        var camera = new Camera3D
        {
            Name = "TestCamera",
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = 25f,
            Position = new Vector3(18f, 22f, 18f)
        };
        AddChild(camera);
        camera.LookAt(IntersectionCenter);
    }

    private sealed class VehicleObservation(TrafficVehicle vehicle)
    {
        public TrafficVehicle Vehicle { get; } = vehicle;
        public bool Entered { get; set; }
        public bool Crossed { get; set; }
    }
}
