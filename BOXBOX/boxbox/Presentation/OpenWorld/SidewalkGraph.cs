using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum SidewalkWaypointKind { Walk, Crosswalk, Activity }

public readonly record struct SidewalkWaypoint(
    Vector3 Position,
    SidewalkWaypointKind Kind,
    TrafficAxis CrossedVehicleAxis = TrafficAxis.NorthSouth);

/// <summary>
/// Deterministic, stream-safe pedestrian graph. It follows the inner sidewalk
/// edge and only crosses roads on the painted crosswalk corridors.
/// </summary>
public static class SidewalkGraph
{
    private const float CellSize = 4f;

    public static List<SidewalkWaypoint> BuildRoute(Vector3 around, uint seed)
    {
        var period = WorldLayoutRules.RoadPeriod * CellSize;
        var bx = Mathf.Floor(around.X / period) * period;
        var bz = Mathf.Floor(around.Z / period) * period;
        // Cells are centred on multiples of four. Roads end close to x/z=6 and
        // the first building cell is centred at 8. Keeping the path at 6.25
        // follows the narrow public sidewalk without cutting through its lot.
        // Separate nearby pedestrians into subtly different walking tracks.
        // The limited offset keeps every route on the narrow sidewalk.
        var trackOffset = ((seed >> 3) % 7u) * .07f - .21f;
        var near = WorldLayoutRules.RoadWidthCells * CellSize - 1.75f + trackOffset;
        var far = period - (CellSize / 2f + .25f) - trackOffset;
        var nextNear = period + near;
        var y = 0.2f;

        List<SidewalkWaypoint> route;
        if ((seed & 1u) == 0)
        {
            route =
            [
                new(new Vector3(bx + near, y, bz + near), SidewalkWaypointKind.Activity),
                new(new Vector3(bx + far, y, bz + near), SidewalkWaypointKind.Walk),
                new(new Vector3(bx + nextNear, y, bz + near), SidewalkWaypointKind.Crosswalk, TrafficAxis.NorthSouth),
                new(new Vector3(bx + period + far, y, bz + near), SidewalkWaypointKind.Activity),
                new(new Vector3(bx + period + far, y, bz + far), SidewalkWaypointKind.Activity),
                new(new Vector3(bx + nextNear, y, bz + far), SidewalkWaypointKind.Walk),
                new(new Vector3(bx + far, y, bz + far), SidewalkWaypointKind.Crosswalk, TrafficAxis.NorthSouth),
                new(new Vector3(bx + near, y, bz + far), SidewalkWaypointKind.Activity)
            ];
        }

        else route =
        [
            new(new Vector3(bx + near, y, bz + near), SidewalkWaypointKind.Activity),
            new(new Vector3(bx + near, y, bz + far), SidewalkWaypointKind.Walk),
            new(new Vector3(bx + near, y, bz + nextNear), SidewalkWaypointKind.Crosswalk, TrafficAxis.EastWest),
            new(new Vector3(bx + near, y, bz + period + far), SidewalkWaypointKind.Activity),
            new(new Vector3(bx + far, y, bz + period + far), SidewalkWaypointKind.Activity),
            new(new Vector3(bx + far, y, bz + nextNear), SidewalkWaypointKind.Walk),
            new(new Vector3(bx + far, y, bz + far), SidewalkWaypointKind.Crosswalk, TrafficAxis.EastWest),
            new(new Vector3(bx + far, y, bz + near), SidewalkWaypointKind.Activity)
        ];

        // Reverse half the routes. The Crosswalk marker belongs to the
        // destination of a crossing segment, so reversal must also remap
        // segment metadata (a naive List.Reverse() would ignore red lights).
        return (seed & 2u) == 0 ? route : ReversePreservingCrosswalkEdges(route);
    }

    private static List<SidewalkWaypoint> ReversePreservingCrosswalkEdges(List<SidewalkWaypoint> route)
    {
        var reverse = new List<SidewalkWaypoint>(route.Count);
        for (var index = 0; index < route.Count; index++)
        {
            var originalIndex = (route.Count - index) % route.Count;
            var source = route[originalIndex];
            var edge = route[(originalIndex + 1) % route.Count];
            reverse.Add(new SidewalkWaypoint(source.Position, edge.Kind, edge.CrossedVehicleAxis));
        }
        return reverse;
    }

    public static Vector3 FindSpawn(Vector3 focus, uint seed)
    {
        var angle = seed * 2.399963f;
        var probe = focus + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 26f;
        var route = BuildRoute(probe, seed);
        // Choose a location along a sidewalk segment instead of repeatedly
        // spawning at the same discrete waypoint. Never place spawns on
        // designated crosswalk segments.
        var nearest = route[0].Position;
        var nearestDistance = float.MaxValue;
        for (var index = 0; index < route.Count; index++)
        {
            var from = route[index];
            var to = route[(index + 1) % route.Count];
            if (from.Kind == SidewalkWaypointKind.Crosswalk ||
                to.Kind == SidewalkWaypointKind.Crosswalk) continue;
            var segment = to.Position - from.Position;
            var lengthSquared = segment.LengthSquared();
            if (lengthSquared < 0.01f) continue;
            var factor = Mathf.Clamp((probe - from.Position).Dot(segment) / lengthSquared, 0.1f, 0.9f);
            var candidate = from.Position + segment * factor;
            var distance = candidate.DistanceSquaredTo(probe);
            if (distance >= nearestDistance) continue;
            nearest = candidate;
            nearestDistance = distance;
        }
        return nearest + Vector3.Up * 0.08f;
    }
}
