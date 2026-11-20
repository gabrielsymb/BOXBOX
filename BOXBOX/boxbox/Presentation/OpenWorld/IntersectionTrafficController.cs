using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum TrafficAxis { NorthSouth, EastWest }
public enum IntersectionPhase { NorthSouthGreen, NorthSouthYellow, AllRedToEastWest, EastWestGreen, EastWestYellow, AllRedToNorthSouth }

public sealed partial class IntersectionTrafficController : Node3D
{
    private const double GreenSeconds = 10.0;
    private const double YellowSeconds = 2.5;
    private const double AllRedSeconds = 1.0;
    private const double ReservationTimeoutSeconds = 6.0;
    // The stop-line center is 4.2 m from the junction center and a traffic car
    // extends about 1.55 m forward. Keeping the half extent below 2.65 m avoids
    // classifying a correctly stopped car as already inside the junction.
    private const float OccupancySize = 5.0f;
    private const float ExitProbeDistance = 5.6f;
    private const float OpposingApproachDistance = 9.5f;
    private const float InsideTieBreakSeconds = 2.0f;

    private readonly List<(TrafficAxis Axis, TrafficLightController Light)> _lights = [];
    private readonly Dictionary<TrafficAxis, int> _crossingPedestrians = new()
    {
        [TrafficAxis.NorthSouth] = 0,
        [TrafficAxis.EastWest] = 0
    };
    private readonly HashSet<TrafficVehicle> _insideVehicles = [];
    private readonly Dictionary<TrafficVehicle, Admission> _admissions = [];
    private readonly Dictionary<TrafficVehicle, double> _insideStoppedSeconds = [];
    private double _remaining = GreenSeconds;
    private double _elapsedSeconds;
    private Area3D? _occupancyArea;

    public IntersectionPhase Phase { get; private set; } = IntersectionPhase.NorthSouthGreen;
    public double TimeRemaining => _remaining;
    public int InsideVehicleCount => _insideVehicles.Count;
    public TrafficLightState StateForAxis(TrafficAxis axis) => StateFor(axis);

    public override void _Ready()
    {
        AddToGroup("intersection_traffic_controller");
        CreateOccupancyArea();
        ApplySignals();
    }

    public override void _Process(double delta)
    {
        _elapsedSeconds += delta;
        RemoveExpiredAdmissions();
        _remaining -= delta;
        if (_remaining > 0) return;
        Phase = Phase switch
        {
            IntersectionPhase.NorthSouthGreen => IntersectionPhase.NorthSouthYellow,
            IntersectionPhase.NorthSouthYellow => IntersectionPhase.AllRedToEastWest,
            IntersectionPhase.AllRedToEastWest => IntersectionPhase.EastWestGreen,
            IntersectionPhase.EastWestGreen => IntersectionPhase.EastWestYellow,
            IntersectionPhase.EastWestYellow => IntersectionPhase.AllRedToNorthSouth,
            _ => IntersectionPhase.NorthSouthGreen
        };
        _remaining = Phase switch
        {
            IntersectionPhase.NorthSouthGreen or IntersectionPhase.EastWestGreen => GreenSeconds,
            IntersectionPhase.NorthSouthYellow or IntersectionPhase.EastWestYellow => YellowSeconds,
            _ => AllRedSeconds
        };
        ApplySignals();
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (var vehicle in _insideVehicles.ToArray())
        {
            if (!IsInstanceValid(vehicle))
            {
                RemoveVehicle(vehicle);
                continue;
            }
            if (vehicle.CurrentSpeed < 0.2f)
                _insideStoppedSeconds[vehicle] = _insideStoppedSeconds.GetValueOrDefault(vehicle) + delta;
            else
                _insideStoppedSeconds[vehicle] = 0;
        }
    }

    public void RegisterLight(TrafficAxis axis, TrafficLightController light)
    {
        _lights.Add((axis, light));
        light.SetState(StateFor(axis));
    }

    public bool ShouldVehicleStop(TrafficAxis axis, float distanceToStopLine)
    {
        if (_crossingPedestrians[axis] > 0) return true;
        var state = StateFor(axis);
        if (state == TrafficLightState.Red) return true;
        return state == TrafficLightState.Yellow && distanceToStopLine > 2.2f;
    }

    public bool TryEnter(TrafficVehicle vehicle, TrafficAxis axis, Vector3 exitDirection)
    {
        if (_admissions.ContainsKey(vehicle) || _insideVehicles.Contains(vehicle)) return true;
        if (StateFor(axis) == TrafficLightState.Red) return false;

        var incomingDirection = vehicle.ActiveIncomingDirection;
        if (!HasExitSpace(vehicle, exitDirection)) return false;
        if (IsLeftTurn(incomingDirection, exitDirection) &&
            HasHigherPriorityOpposingVehicle(vehicle, incomingDirection)) return false;

        // Conservative first slice: one autonomous admission at a time. Later
        // tests may admit proven non-conflicting paths without weakening guards.
        if (_insideVehicles.Any(other => other != vehicle) ||
            _admissions.Keys.Any(other => other != vehicle)) return false;

        _admissions[vehicle] = new Admission(axis, incomingDirection, exitDirection, _elapsedSeconds);
        GD.Print($"JUNCTION_ENTER_GRANTED id={vehicle.DiagnosticId} axis={axis} exit={CardinalName(exitDirection)}");
        return true;
    }

    public bool IsInside(TrafficVehicle vehicle) => _insideVehicles.Contains(vehicle);

    public bool MayProceedInside(TrafficVehicle vehicle)
    {
        if (!_insideVehicles.Contains(vehicle)) return true;
        var stalled = _insideVehicles
            .Where(candidate => IsInstanceValid(candidate) &&
                                _insideStoppedSeconds.GetValueOrDefault(candidate) > InsideTieBreakSeconds)
            .ToArray();
        if (stalled.Length < 2) return true;
        var winnerId = stalled.Min(candidate => candidate.GetInstanceId());
        return vehicle.GetInstanceId() == winnerId;
    }

    public void AppendInsideVehicleExclusions(Godot.Collections.Array<Rid> exclusions,
        TrafficVehicle observer)
    {
        if (!_insideVehicles.Contains(observer)) return;
        foreach (var vehicle in _insideVehicles)
            if (vehicle != observer && IsInstanceValid(vehicle)) exclusions.Add(vehicle.GetRid());
    }

    public bool CanPedestrianStart(TrafficAxis crossedVehicleAxis, float crossingSeconds = 4.5f) =>
        StateFor(crossedVehicleAxis) == TrafficLightState.Red && _remaining >= crossingSeconds;

    public void BeginPedestrianCrossing(TrafficAxis axis) => _crossingPedestrians[axis]++;
    public void EndPedestrianCrossing(TrafficAxis axis) =>
        _crossingPedestrians[axis] = Math.Max(0, _crossingPedestrians[axis] - 1);

    public static TrafficAxis AxisFor(Vector3 direction) =>
        MathF.Abs(direction.Z) >= MathF.Abs(direction.X) ? TrafficAxis.NorthSouth : TrafficAxis.EastWest;

    public static IntersectionTrafficController? FindApproaching(
        SceneTree tree, Vector3 position, Vector3 direction, out float distanceToStopLine)
    {
        IntersectionTrafficController? best = null;
        distanceToStopLine = float.MaxValue;
        foreach (var node in tree.GetNodesInGroup("intersection_traffic_controller"))
        {
            if (node is not IntersectionTrafficController candidate) continue;
            var toward = candidate.GlobalPosition - position;
            toward.Y = 0;
            var along = toward.Dot(direction.Normalized());
            var lateral = toward - direction.Normalized() * along;
            var stopDistance = along - 4.2f;
            if (along <= 4.2f || stopDistance > 10f || lateral.Length() > 4f || stopDistance >= distanceToStopLine) continue;
            best = candidate;
            distanceToStopLine = stopDistance;
        }
        return best;
    }

    public static IntersectionTrafficController? FindNearestCrossing(
        SceneTree tree, Vector3 crossingMidpoint, float maximumDistance = 12f)
    {
        IntersectionTrafficController? best = null;
        var bestDistance = maximumDistance;
        foreach (var node in tree.GetNodesInGroup("intersection_traffic_controller"))
        {
            if (node is not IntersectionTrafficController candidate) continue;
            var offset = candidate.GlobalPosition - crossingMidpoint;
            offset.Y = 0;
            var distance = offset.Length();
            if (distance >= bestDistance) continue;
            best = candidate;
            bestDistance = distance;
        }
        return best;
    }

    private void CreateOccupancyArea()
    {
        _occupancyArea = new Area3D
        {
            Name = "JunctionOccupancy",
            Position = Vector3.Up * 0.8f,
            CollisionLayer = 0,
            CollisionMask = OpenWorldCollisionLayers.TrafficVehicle,
            Monitoring = true,
            Monitorable = false
        };
        _occupancyArea.AddChild(new CollisionShape3D
        {
            Name = "JunctionOccupancyShape",
            Shape = new BoxShape3D { Size = new Vector3(OccupancySize, 2f, OccupancySize) }
        });
        _occupancyArea.BodyEntered += OnBodyEntered;
        _occupancyArea.BodyExited += OnBodyExited;
        AddChild(_occupancyArea);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not TrafficVehicle vehicle) return;
        _insideVehicles.Add(vehicle);
        _insideStoppedSeconds[vehicle] = 0;
        vehicle.OnEnteredIntersection(this);
        GD.Print($"JUNCTION_INSIDE id={vehicle.DiagnosticId} count={_insideVehicles.Count}");
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not TrafficVehicle vehicle) return;
        RemoveVehicle(vehicle);
        vehicle.OnExitedIntersection(this);
        GD.Print($"JUNCTION_EXIT id={vehicle.DiagnosticId} count={_insideVehicles.Count}");
    }

    private void RemoveVehicle(TrafficVehicle vehicle)
    {
        _insideVehicles.Remove(vehicle);
        _insideStoppedSeconds.Remove(vehicle);
        _admissions.Remove(vehicle);
    }

    private void RemoveExpiredAdmissions()
    {
        foreach (var (vehicle, admission) in _admissions.ToArray())
        {
            if (IsInstanceValid(vehicle) && (_insideVehicles.Contains(vehicle) ||
                _elapsedSeconds - admission.GrantedAtSeconds <= ReservationTimeoutSeconds)) continue;
            _admissions.Remove(vehicle);
            if (IsInstanceValid(vehicle)) vehicle.OnAdmissionExpired(this);
        }
    }

    private bool HasExitSpace(TrafficVehicle vehicle, Vector3 exitDirection)
    {
        var direction = Cardinal(exitDirection);
        var center = GlobalPosition + RoadNodeGraph.LaneOffsetFor(direction) + direction * ExitProbeDistance;
        center.Y = vehicle.GlobalPosition.Y + 0.62f;
        var yaw = Mathf.Atan2(-direction.X, -direction.Z);
        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = new BoxShape3D { Size = new Vector3(1.55f, 1.2f, 3.4f) },
            Transform = new Transform3D(new Basis(Vector3.Up, yaw), center),
            CollisionMask = OpenWorldCollisionLayers.TrafficVehicle |
                            OpenWorldCollisionLayers.DriveableVehicle,
            Margin = 0.05f,
            Exclude = new Godot.Collections.Array<Rid> { vehicle.GetRid() }
        };
        return GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
    }

    private bool HasHigherPriorityOpposingVehicle(TrafficVehicle vehicle, Vector3 incomingDirection)
    {
        foreach (var node in GetTree().GetNodesInGroup("traffic_vehicle"))
        {
            if (node is not TrafficVehicle other || other == vehicle || !IsInstanceValid(other)) continue;
            var offset = other.GlobalPosition - GlobalPosition;
            offset.Y = 0;
            if (offset.Length() > OpposingApproachDistance) continue;
            if (other.ActiveIncomingDirection.Dot(-incomingDirection) < 0.8f) continue;
            if (_insideVehicles.Contains(other) || _admissions.ContainsKey(other)) return true;
            if (other.DiagnosticId < vehicle.DiagnosticId) return true;
        }
        return false;
    }

    private static bool IsLeftTurn(Vector3 incoming, Vector3 outgoing) =>
        Vector3.Up.Cross(Cardinal(incoming)).Dot(Cardinal(outgoing)) > 0.8f;

    private static Vector3 Cardinal(Vector3 direction)
    {
        if (MathF.Abs(direction.X) > MathF.Abs(direction.Z))
            return direction.X >= 0 ? Vector3.Right : Vector3.Left;
        return direction.Z >= 0 ? Vector3.Back : Vector3.Forward;
    }

    private static string CardinalName(Vector3 direction)
    {
        var cardinal = Cardinal(direction);
        if (cardinal == Vector3.Right) return "E";
        if (cardinal == Vector3.Left) return "W";
        return cardinal == Vector3.Back ? "S" : "N";
    }

    private TrafficLightState StateFor(TrafficAxis axis) => Phase switch
    {
        IntersectionPhase.NorthSouthGreen when axis == TrafficAxis.NorthSouth => TrafficLightState.Green,
        IntersectionPhase.NorthSouthYellow when axis == TrafficAxis.NorthSouth => TrafficLightState.Yellow,
        IntersectionPhase.EastWestGreen when axis == TrafficAxis.EastWest => TrafficLightState.Green,
        IntersectionPhase.EastWestYellow when axis == TrafficAxis.EastWest => TrafficLightState.Yellow,
        _ => TrafficLightState.Red
    };

    private void ApplySignals()
    {
        foreach (var (axis, light) in _lights) light.SetState(StateFor(axis));
    }

    private readonly record struct Admission(
        TrafficAxis Axis,
        Vector3 IncomingDirection,
        Vector3 ExitDirection,
        double GrantedAtSeconds);
}
