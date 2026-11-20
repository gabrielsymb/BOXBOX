using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum TrafficAxis { NorthSouth, EastWest }
public enum IntersectionPhase { NorthSouthGreen, NorthSouthYellow, AllRedToEastWest, EastWestGreen, EastWestYellow, AllRedToNorthSouth }

public sealed partial class IntersectionTrafficController : Node3D
{
    private const double GreenSeconds = 10.0;
    private const double YellowSeconds = 2.5;
    private const double AllRedSeconds = 1.0;
    private static readonly (IntersectionPhase Phase, double Duration)[] Schedule =
    [
        (IntersectionPhase.NorthSouthGreen, GreenSeconds),
        (IntersectionPhase.NorthSouthYellow, YellowSeconds),
        (IntersectionPhase.AllRedToEastWest, AllRedSeconds),
        (IntersectionPhase.EastWestGreen, GreenSeconds),
        (IntersectionPhase.EastWestYellow, YellowSeconds),
        (IntersectionPhase.AllRedToNorthSouth, AllRedSeconds)
    ];
    private const double CycleSeconds = 2 * (GreenSeconds + YellowSeconds + AllRedSeconds);
    private readonly List<(TrafficAxis Axis, TrafficLightController Light)> _lights = [];
    private readonly Dictionary<TrafficAxis, int> _crossingPedestrians = new()
    {
        [TrafficAxis.NorthSouth] = 0,
        [TrafficAxis.EastWest] = 0
    };
    private double _remaining = GreenSeconds;
    private TrafficVehicle? _reservedVehicle;
    private TrafficAxis _reservedAxis;
    private bool _reservedVehicleEntered;
    private double _reservationStarted;

    public IntersectionPhase Phase { get; private set; } = IntersectionPhase.NorthSouthGreen;
    public double TimeRemaining => _remaining;
    public TrafficLightState StateForAxis(TrafficAxis axis) => StateFor(axis);

    public override void _Ready()
    {
        AddToGroup("intersection_traffic_controller");
        SynchronizePhase();
        ApplySignals();
    }

    public override void _Process(double delta)
    {
        SynchronizePhase();
        CleanupVehicleReservation();
    }

    private void SynchronizePhase()
    {
        // The engine clock survives streaming/unloading of individual city cells.
        // Recreating a visual intersection must not restart its traffic cycle.
        var elapsed = Time.GetTicksMsec() / 1000.0 % CycleSeconds;
        foreach (var (phase, duration) in Schedule)
        {
            if (elapsed >= duration)
            {
                elapsed -= duration;
                continue;
            }

            _remaining = duration - elapsed;
            if (Phase != phase)
            {
                Phase = phase;
                ApplySignals();
            }
            return;
        }
    }

    public void RegisterLight(TrafficAxis axis, TrafficLightController light)
    {
        _lights.Add((axis, light));
        light.SetState(StateFor(axis));
    }

    // Street furniture is replaceable; junction occupancy is not.
    public void ClearLights() => _lights.Clear();

    public bool ShouldVehicleStop(TrafficAxis axis, float distanceToStopLine)
    {
        if (_crossingPedestrians[axis] > 0) return true;
        var state = StateFor(axis);
        if (state == TrafficLightState.Red) return true;
        // On yellow, vehicles that can still stop comfortably must do so.
        return state == TrafficLightState.Yellow && distanceToStopLine > 2.2f;
    }

    /// <summary>
    /// A traffic signal grants the right-of-way, but only one AI vehicle may
    /// occupy the crossing box at a time. This is intentionally conservative:
    /// turn-to-turn conflicts must never rely on collision physics alone.
    /// </summary>
    public bool MayVehicleEnter(TrafficVehicle vehicle, TrafficAxis axis, float distanceToStopLine)
    {
        CleanupVehicleReservation();
        if (_reservedVehicle == vehicle) return true;
        if (_reservedVehicle is not null || ShouldVehicleStop(axis, distanceToStopLine)) return false;

        // Do not monopolize the junction while still far away from the stop line.
        if (distanceToStopLine > 2.8f) return true;

        _reservedVehicle = vehicle;
        _reservedAxis = axis;
        _reservedVehicleEntered = false;
        _reservationStarted = Time.GetTicksMsec() / 1000.0;
        return true;
    }

    public bool HasVehicleReservation(TrafficVehicle vehicle)
    {
        CleanupVehicleReservation();
        return _reservedVehicle == vehicle;
    }

    public void ReleaseVehicle(TrafficVehicle vehicle)
    {
        if (_reservedVehicle == vehicle) ClearVehicleReservation();
    }

    private void CleanupVehicleReservation()
    {
        if (_reservedVehicle is null) return;
        if (!IsInstanceValid(_reservedVehicle) || !_reservedVehicle.IsInsideTree() ||
            _reservedVehicle.IsQueuedForDeletion())
        {
            ClearVehicleReservation();
            return;
        }

        var distance = _reservedVehicle.GlobalPosition.DistanceTo(GlobalPosition);
        if (distance <= 4.8f) _reservedVehicleEntered = true;
        // An authorization cannot outlive a red phase while its vehicle is
        // still on the approach. Cars already in the box keep their clearance.
        if (!_reservedVehicleEntered && ScheduledStateFor(_reservedAxis) == TrafficLightState.Red)
        {
            ClearVehicleReservation();
            return;
        }
        // A car is clear only after it has traversed the junction, not while
        // approaching the stop line. Release a stalled approach after a grace period.
        var waited = Time.GetTicksMsec() / 1000.0 - _reservationStarted;
        if ((_reservedVehicleEntered && distance > 7.0f) ||
            (!_reservedVehicleEntered && waited > 6.0))
            ClearVehicleReservation();
    }

    private void ClearVehicleReservation()
    {
        _reservedVehicle = null;
        _reservedVehicleEntered = false;
    }

    public bool CanPedestrianStart(TrafficAxis crossedVehicleAxis, float crossingSeconds = 4.5f)
    {
        CleanupVehicleReservation();
        // Check the scheduled phase, not the overridden visual state: active
        // crossings can force the lens red without opening a new walk window.
        return _reservedVehicle is null &&
            ScheduledStateFor(crossedVehicleAxis) == TrafficLightState.Red &&
            _remaining >= crossingSeconds;
    }

    public void BeginPedestrianCrossing(TrafficAxis axis)
    {
        _crossingPedestrians[axis]++;
        ApplySignals();
    }

    public void EndPedestrianCrossing(TrafficAxis axis)
    {
        _crossingPedestrians[axis] = Math.Max(0, _crossingPedestrians[axis] - 1);
        ApplySignals();
    }

    public static TrafficAxis AxisFor(Vector3 direction) =>
        MathF.Abs(direction.Z) >= MathF.Abs(direction.X) ? TrafficAxis.NorthSouth : TrafficAxis.EastWest;

    public static IntersectionTrafficController? FindApproaching(
        SceneTree tree, Vector3 position, Vector3 direction, out float distanceToStopLine)
    {
        IntersectionTrafficController? best = null;
        distanceToStopLine = float.MaxValue;
        foreach (var node in tree.GetNodesInGroup("intersection_traffic_controller"))
        {
            if (node is not IntersectionTrafficController candidate || !candidate.IsInsideTree() ||
                candidate.IsQueuedForDeletion()) continue;
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
            if (node is not IntersectionTrafficController candidate || !candidate.IsInsideTree() ||
                candidate.IsQueuedForDeletion()) continue;
            var offset = candidate.GlobalPosition - crossingMidpoint;
            offset.Y = 0;
            var distance = offset.Length();
            if (distance >= bestDistance) continue;
            best = candidate;
            bestDistance = distance;
        }
        return best;
    }

    private TrafficLightState StateFor(TrafficAxis axis) =>
        _crossingPedestrians[axis] > 0 ? TrafficLightState.Red : ScheduledStateFor(axis);

    private TrafficLightState ScheduledStateFor(TrafficAxis axis) => Phase switch
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
}
