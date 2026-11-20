using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum TrafficAxis { NorthSouth, EastWest }
public enum IntersectionPhase { NorthSouthGreen, NorthSouthYellow, AllRedToEastWest, EastWestGreen, EastWestYellow, AllRedToNorthSouth }

public sealed partial class IntersectionTrafficController : Node3D
{
    private const double GreenSeconds = 10.0;
    private const double YellowSeconds = 2.5;
    private const double AllRedSeconds = 1.0;
    private readonly List<(TrafficAxis Axis, TrafficLightController Light)> _lights = [];
    private readonly Dictionary<TrafficAxis, int> _crossingPedestrians = new()
    {
        [TrafficAxis.NorthSouth] = 0,
        [TrafficAxis.EastWest] = 0
    };
    private double _remaining = GreenSeconds;

    public IntersectionPhase Phase { get; private set; } = IntersectionPhase.NorthSouthGreen;
    public double TimeRemaining => _remaining;
    public TrafficLightState StateForAxis(TrafficAxis axis) => StateFor(axis);

    public override void _Ready()
    {
        AddToGroup("intersection_traffic_controller");
        ApplySignals();
    }

    public override void _Process(double delta)
    {
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
        // On yellow, vehicles that can still stop comfortably must do so.
        return state == TrafficLightState.Yellow && distanceToStopLine > 2.2f;
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
}
