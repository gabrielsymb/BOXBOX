using Godot;

namespace Boxbox.Presentation.OpenWorld;

public static class RoadNodeGraph
{
    public const float RoadSpacing = WorldLayoutRules.RoadPeriod * 4f;
    public const float RoadCenterOffset = 2f;
    public const float LaneOffset = 1.45f;
    private const float TurnRadius = 5.2f;

    /// <summary>
    /// Projects an arbitrary world position onto the legal directional lane.
    /// The lane offset is shared with maneuver generation, so spawned cars
    /// enter the same topology they will follow.
    /// </summary>
    public static Vector3 SnapToLane(Vector3 position, Vector3 direction)
    {
        var laneOffset = LaneOffsetFor(direction);
        if (MathF.Abs(direction.X) > MathF.Abs(direction.Z))
            return new Vector3(position.X, position.Y,
                NearestRoadCenter(position.Z) + laneOffset.Z);
        return new Vector3(NearestRoadCenter(position.X) + laneOffset.X,
            position.Y, position.Z);
    }

    public static float DistanceToNearestJunction(Vector3 position, Vector3 direction)
    {
        var travelPosition = MathF.Abs(direction.X) > MathF.Abs(direction.Z)
            ? position.X : position.Z;
        return MathF.Abs(travelPosition - NearestRoadCenter(travelPosition));
    }

    private static float NearestRoadCenter(float value) =>
        RoadCenterOffset + Mathf.Round((value - RoadCenterOffset) / RoadSpacing) * RoadSpacing;

    public static void AppendNextManeuver(
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
            waypoints.Add(incomingLane + direction * (TurnRadius + 1.8f));
            return;
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
        direction = outgoing;
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
