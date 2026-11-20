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
        var near = WorldLayoutRules.RoadWidthCells * CellSize - 1.75f;
        var far = period - (CellSize / 2f + .25f);
        var nextNear = period + near;
        var y = 0.2f;

        if ((seed & 1u) == 0)
        {
            return
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

        return
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
    }

    public static Vector3 FindSpawn(Vector3 focus, uint seed)
    {
        var angle = seed * 2.399963f;
        var probe = focus + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 26f;
        var route = BuildRoute(probe, seed);
        var nearest = route[0].Position;
        var nearestDistance = float.MaxValue;
        foreach (var waypoint in route)
        {
            var distance = waypoint.Position.DistanceSquaredTo(probe);
            if (distance >= nearestDistance) continue;
            nearest = waypoint.Position;
            nearestDistance = distance;
        }
        return nearest + Vector3.Up * 0.08f;
    }
}
