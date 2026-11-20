using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class TrafficSpawner : Node3D
{
    private const int MaximumVehicles = 4;
    private const float SpawnInterval = 3f;
    private const float SpawnDistance = 24f;
    private const float DespawnDistance = 42f;
    private readonly List<TrafficVehicle> _vehicles = [];
    private double _cooldown;
    private uint _sequence;
    public PlayerController? Focus { get; set; }

    public override void _Process(double delta)
    {
        if (Focus is null) return;
        _cooldown -= delta;
        RemoveDistantVehicles(Focus.FocusPosition);
        if (_cooldown > 0 || _vehicles.Count >= MaximumVehicles) return;
        _cooldown = SpawnInterval;
        SpawnVehicle(Focus.FocusPosition);
    }

    private void SpawnVehicle(Vector3 focus)
    {
        _sequence++;
        var positive = _sequence % 2 == 0;
        var roadX = Mathf.Round((focus.X - RoadNodeGraph.RoadCenterOffset) / RoadNodeGraph.RoadSpacing) *
                    RoadNodeGraph.RoadSpacing + RoadNodeGraph.RoadCenterOffset;
        var direction = positive ? Vector3.Back : Vector3.Forward;
        var position = new Vector3(
            roadX + (positive ? -RoadNodeGraph.LaneOffset : RoadNodeGraph.LaneOffset),
            0.35f,
            focus.Z + (positive ? -SpawnDistance : SpawnDistance));

        if (_vehicles.Any(vehicle => vehicle.GlobalPosition.DistanceTo(position) < 7f)) return;
        var vehicle = new TrafficVehicle { Position = position };
        var visualPath = WorldAssetCatalog.Current.Pick(WorldAssetCategory.Vehicles, _sequence);
        vehicle.Initialize(direction, visualPath, _sequence * 104729);
        AddChild(vehicle);
        _vehicles.Add(vehicle);
    }

    private void RemoveDistantVehicles(Vector3 focus)
    {
        for (var index = _vehicles.Count - 1; index >= 0; index--)
        {
            var vehicle = _vehicles[index];
            if (!IsInstanceValid(vehicle) || vehicle.GlobalPosition.DistanceTo(focus) <= DespawnDistance) continue;
            vehicle.QueueFree();
            _vehicles.RemoveAt(index);
        }
    }
}
