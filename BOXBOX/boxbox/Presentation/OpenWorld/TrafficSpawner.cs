using Godot;

namespace Boxbox.Presentation.OpenWorld;

/// <summary>
/// Creates only off-camera traffic, aligned with a legal lane and away from
/// junction boxes. Traffic agents remain lightweight presentation objects;
/// their spawn randomness never mutates the persistent economic simulation.
/// </summary>
public sealed partial class TrafficSpawner : Node3D
{
    private const int MaximumVehicles = 7;
    private const float SpawnInterval = 2.5f;
    private const float MinimumSpawnDistance = 19f;
    private const float MaximumSpawnDistance = 35f;
    private const float DespawnDistance = 43f;
    private const float MinimumVehicleSpacing = 7.5f;
    private const float JunctionExclusionDistance = 9f;
    private readonly List<TrafficVehicle> _vehicles = [];
    private double _cooldown;
    private uint _sequence;

    public PlayerController? Focus { get; set; }

    public override void _Process(double delta)
    {
        if (Focus is null) return;

        RemoveDistantVehicles(Focus.FocusPosition);
        _cooldown -= delta;
        if (_cooldown > 0 || _vehicles.Count >= MaximumVehicles) return;
        _cooldown = SpawnInterval;
        TrySpawnVehicle(Focus.FocusPosition);
    }

    private void TrySpawnVehicle(Vector3 focus)
    {
        var camera = GetViewport().GetCamera3D();
        // The player may exist for a few frames before the camera is ready.
        if (camera is null) return;
        var visibleArea = GetViewport().GetVisibleRect().Grow(100f);

        for (uint attempt = 0; attempt < 24; attempt++)
        {
            var seed = _sequence * 747796405u + attempt * 2891336453u;
            var direction = (seed & 3u) switch
            {
                0 => Vector3.Forward,
                1 => Vector3.Back,
                2 => Vector3.Left,
                _ => Vector3.Right
            };

            // Probe all around the player and project onto the appropriate
            // road, rather than repeatedly spawning along one vertical road.
            var angle = ((seed >> 4) % 1024) / 1024f * Mathf.Tau;
            var radius = 26f + (seed % 9);
            var probe = focus + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            var position = RoadNodeGraph.SnapToLane(probe, direction);
            position.Y = 0.35f;

            var distance = position.DistanceTo(focus);
            if (distance < MinimumSpawnDistance || distance > MaximumSpawnDistance) continue;
            if (RoadNodeGraph.DistanceToNearestJunction(position, direction) < JunctionExclusionDistance) continue;
            if (!camera.IsPositionBehind(position) && visibleArea.HasPoint(camera.UnprojectPosition(position))) continue;
            if (OverlapsExistingVehicle(position)) continue;

            var vehicle = new TrafficVehicle { Position = position };
            var visualPath = WorldAssetCatalog.Current.Pick(WorldAssetCategory.Vehicles, _sequence + 1);
            vehicle.Initialize(direction, visualPath, (_sequence + 1) * 104729u);
            AddChild(vehicle);
            _vehicles.Add(vehicle);
            _sequence += attempt + 1;
            return;
        }
        // Change the candidate set next time even when every off-camera lane
        // was occupied; never force a visibly overlapping spawn.
        _sequence += 24;
    }

    private bool OverlapsExistingVehicle(Vector3 position)
    {
        foreach (var group in new[] { "traffic_vehicle", "driveable_car" })
        foreach (var node in GetTree().GetNodesInGroup(group))
        {
            if (node is not Node3D other || !IsInstanceValid(other) || other.IsQueuedForDeletion()) continue;
            var distance = other.GlobalPosition - position;
            distance.Y = 0;
            if (distance.LengthSquared() < MinimumVehicleSpacing * MinimumVehicleSpacing) return true;
        }
        return false;
    }

    private void RemoveDistantVehicles(Vector3 focus)
    {
        for (var index = _vehicles.Count - 1; index >= 0; index--)
        {
            var vehicle = _vehicles[index];
            if (!IsInstanceValid(vehicle) || vehicle.IsQueuedForDeletion())
            {
                _vehicles.RemoveAt(index);
                continue;
            }
            if (vehicle.GlobalPosition.DistanceTo(focus) <= DespawnDistance) continue;
            vehicle.QueueFree();
            _vehicles.RemoveAt(index);
        }
    }
}
