using Boxbox.Core.Traffic;

namespace Boxbox.Verification;

/// <summary>
/// Road-system invariants verified without a renderer, physics server,
/// external navigation service, or a running Godot instance.
/// </summary>
internal static class TrafficLaneGridChecks
{
    private static readonly TrafficLaneGrid Grid = new(32f, 2f, 1.45f);

    public static void OppositeFlowsUseSeparateLanes()
    {
        var north = Grid.SnapToLane(new TrafficCoordinate(12f, 18f), CardinalRoadDirection.North);
        var south = Grid.SnapToLane(new TrafficCoordinate(12f, 18f), CardinalRoadDirection.South);
        var east = Grid.SnapToLane(new TrafficCoordinate(18f, 12f), CardinalRoadDirection.East);
        var west = Grid.SnapToLane(new TrafficCoordinate(18f, 12f), CardinalRoadDirection.West);

        Near(north.X, 3.45f, "northbound lane");
        Near(south.X, .55f, "southbound lane");
        Near(east.Z, 3.45f, "eastbound lane");
        Near(west.Z, .55f, "westbound lane");
        Near(north.Z, 18f, "northbound progress coordinate must be preserved");
        Near(east.X, 18f, "eastbound progress coordinate must be preserved");
        VerificationAssert.Require(north.X - south.X > 2.8f, "Opposing vertical lanes overlap");
        VerificationAssert.Require(east.Z - west.Z > 2.8f, "Opposing horizontal lanes overlap");
    }

    public static void EveryManeuverTargetsAJunctionAhead()
    {
        var north = Grid.NextIntersection(new TrafficCoordinate(3.45f, 20f), CardinalRoadDirection.North);
        var south = Grid.NextIntersection(new TrafficCoordinate(.55f, -20f), CardinalRoadDirection.South);
        var east = Grid.NextIntersection(new TrafficCoordinate(10f, 3.45f), CardinalRoadDirection.East);
        var west = Grid.NextIntersection(new TrafficCoordinate(-20f, .55f), CardinalRoadDirection.West);

        Near(north.X, 2f, "north intersection center X");
        Near(north.Z, 2f, "north intersection center Z");
        Near(south.X, 2f, "south intersection center X");
        Near(south.Z, 2f, "south intersection center Z");
        Near(east.X, 34f, "east intersection center X");
        Near(east.Z, 2f, "east intersection center Z");
        Near(west.X, -30f, "west intersection center X");
        Near(west.Z, 2f, "west intersection center Z");

        VerificationAssert.Require(north.Z < 20f && south.Z > -20f &&
            east.X > 10f && west.X < -20f, "A route maneuver points behind the moving car");
    }

    public static void JunctionCenterDoesNotRetargetItself()
    {
        var north = Grid.NextIntersection(new TrafficCoordinate(3.45f, 2f), CardinalRoadDirection.North);
        var east = Grid.NextIntersection(new TrafficCoordinate(2f, 3.45f), CardinalRoadDirection.East);
        Near(north.Z, -30f, "north from exact center should reach preceding grid intersection");
        Near(east.X, 34f, "east from exact center should reach next grid intersection");
    }

    public static void SpawnExclusionAccountsForNegativeCoordinates()
    {
        Near(Grid.DistanceToNearestJunction(new TrafficCoordinate(3.45f, 34f), CardinalRoadDirection.North),
            0f, "northbound junction at +34");
        Near(Grid.DistanceToNearestJunction(new TrafficCoordinate(.55f, -30f), CardinalRoadDirection.South),
            0f, "southbound junction at -30");
        Near(Grid.DistanceToNearestJunction(new TrafficCoordinate(18f, 3.45f), CardinalRoadDirection.East),
            16f, "midblock position");
        Near(Grid.DistanceToNearestJunction(new TrafficCoordinate(-21f, .55f), CardinalRoadDirection.West),
            9f, "western approach to -30 junction");
    }

    private static void Near(float actual, float expected, string context) =>
        VerificationAssert.Require(MathF.Abs(actual - expected) < .001f,
            $"{context}: expected {expected}, actual {actual}");
}
