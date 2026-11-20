using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum RoadManeuverKind { Straight, Left, Right }

public readonly record struct RoadManeuver(
    Vector3 IntersectionCenter,
    Vector3 IncomingDirection,
    Vector3 ExitDirection,
    Vector3 ExitPoint,
    RoadManeuverKind Kind)
{
    public bool HasExited(Vector3 position)
    {
        var offset = position - IntersectionCenter;
        offset.Y = 0;
        return offset.Dot(ExitDirection) >= 4.65f;
    }
}

public static class RoadNodeGraph
{
    public const float RoadSpacing = WorldLayoutRules.RoadPeriod * 4f;
    public const float RoadCenterOffset = 2f;
    public const float LaneOffset = 1.45f;
    private const float TurnRadius = 5.2f;

    public static RoadManeuver AppendNextManeuver(
        List<Vector3> waypoints,
        Vector3 from,
        ref Vector3 direction,
        uint decisionSeed)
    {
        direction = Cardinal(direction);
        var center = NextIntersection(from, direction);
        center.Y = from.Y;
        var incomingLane = center + LaneOffsetFor(direction);
        var start = incomingLane - direction * TurnRadius;
        if ((start - from).Dot(direction) > 0.5f)
            waypoints.Add(start);

        var choice = decisionSeed % 5;
        if (choice >= 2)
        {
            var straightEnd = incomingLane + direction * (TurnRadius + 1.8f);
            waypoints.Add(straightEnd);
            return new RoadManeuver(center, direction, direction, straightEnd, RoadManeuverKind.Straight);
        }

        var outgoing = choice == 0 ? TurnLeft(direction) : TurnRight(direction);
        var outgoingLane = center + LaneOffsetFor(outgoing);
        var end = outgoingLane + outgoing * TurnRadius;
        var control = MathF.Abs(direction.X) > 0.5f
            ? new Vector3(outgoingLane.X, from.Y, incomingLane.Z)
            : new Vector3(incomingLane.X, from.Y, outgoingLane.Z);

        for (var index = 1; index <= 6; index++)
        {
            var t = index / 6f;
            waypoints.Add(QuadraticBezier(start, control, end, t));
        }
        var maneuver = new RoadManeuver(center, direction, outgoing, end,
            choice == 0 ? RoadManeuverKind.Left : RoadManeuverKind.Right);
        direction = outgoing;
        return maneuver;
    }

    public static Vector3 LaneOffsetFor(Vector3 direction) =>
        Cardinal(direction).Cross(Vector3.Up) * LaneOffset;

    private static Vector3 NextIntersection(Vector3 from, Vector3 direction)
    {
        var roadCenter = from - LaneOffsetFor(direction);
        if (MathF.Abs(direction.X) > 0.5f)
        {
            var index = (from.X - RoadCenterOffset) / RoadSpacing;
            var nextIndex = direction.X > 0 ? MathF.Floor(index) + 1 : MathF.Ceiling(index) - 1;
            return new Vector3(
                RoadCenterOffset + nextIndex * RoadSpacing,
                from.Y,
                RoadCenterOffset + MathF.Round((roadCenter.Z - RoadCenterOffset) / RoadSpacing) * RoadSpacing);
        }

        var zIndex = (from.Z - RoadCenterOffset) / RoadSpacing;
        var nextZIndex = direction.Z > 0 ? MathF.Floor(zIndex) + 1 : MathF.Ceiling(zIndex) - 1;
        return new Vector3(
            RoadCenterOffset + MathF.Round((roadCenter.X - RoadCenterOffset) / RoadSpacing) * RoadSpacing,
            from.Y,
            RoadCenterOffset + nextZIndex * RoadSpacing);
    }

    private static Vector3 Cardinal(Vector3 direction)
    {
        if (MathF.Abs(direction.X) > MathF.Abs(direction.Z))
            return direction.X >= 0 ? Vector3.Right : Vector3.Left;
        return direction.Z >= 0 ? Vector3.Back : Vector3.Forward;
    }

    private static Vector3 TurnRight(Vector3 direction) => direction.Cross(Vector3.Up).Normalized();
    private static Vector3 TurnLeft(Vector3 direction) => Vector3.Up.Cross(direction).Normalized();

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        var inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }
}
