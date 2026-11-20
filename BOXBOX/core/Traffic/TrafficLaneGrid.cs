namespace Boxbox.Core.Traffic;

/// <summary>
/// The direction of a legally aligned one-way lane within a two-way road.
/// This geometric model is independent of Godot and can be verified headlessly.
/// </summary>
public enum CardinalRoadDirection { North, South, East, West }

public readonly record struct TrafficCoordinate(float X, float Z);

/// <summary>
/// Deterministic lane topology shared by the world spawn and route planner.
/// Road centers repeat every RoadSpacing units on both axes; a lane has a
/// directional offset from the central line of the two-way road.
/// </summary>
public readonly record struct TrafficLaneGrid(
    float RoadSpacing, float RoadCenterOffset, float LaneOffset)
{
    public TrafficCoordinate SnapToLane(TrafficCoordinate probe, CardinalRoadDirection direction) =>
        direction switch
        {
            CardinalRoadDirection.North => probe with { X = NearestCenter(probe.X) + LaneOffset },
            CardinalRoadDirection.South => probe with { X = NearestCenter(probe.X) - LaneOffset },
            CardinalRoadDirection.East => probe with { Z = NearestCenter(probe.Z) + LaneOffset },
            CardinalRoadDirection.West => probe with { Z = NearestCenter(probe.Z) - LaneOffset },
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };

    public float DistanceToNearestJunction(TrafficCoordinate position, CardinalRoadDirection direction)
    {
        var axis = direction is CardinalRoadDirection.East or CardinalRoadDirection.West
            ? position.X : position.Z;
        return MathF.Abs(axis - NearestCenter(axis));
    }

    /// <summary>
    /// The next *strictly forward* intersection, even when the starting
    /// position is exactly on a junction center. This prevents U-turn route
    /// targets when a route is extended past its last waypoint.
    /// </summary>
    public TrafficCoordinate NextIntersection(TrafficCoordinate from, CardinalRoadDirection direction)
    {
        if (direction is CardinalRoadDirection.East or CardinalRoadDirection.West)
        {
            var index = (from.X - RoadCenterOffset) / RoadSpacing;
            var next = direction == CardinalRoadDirection.East
                ? MathF.Floor(index) + 1f
                : MathF.Ceiling(index) - 1f;
            var laneZ = direction == CardinalRoadDirection.East ? LaneOffset : -LaneOffset;
            return new TrafficCoordinate(
                RoadCenterOffset + next * RoadSpacing,
                NearestCenter(from.Z - laneZ));
        }

        var zIndex = (from.Z - RoadCenterOffset) / RoadSpacing;
        var nextZ = direction == CardinalRoadDirection.South
            ? MathF.Floor(zIndex) + 1f
            : MathF.Ceiling(zIndex) - 1f;
        var laneX = direction == CardinalRoadDirection.North ? LaneOffset : -LaneOffset;
        return new TrafficCoordinate(
            NearestCenter(from.X - laneX),
            RoadCenterOffset + nextZ * RoadSpacing);
    }

    private float NearestCenter(float position) =>
        RoadCenterOffset +
        MathF.Round((position - RoadCenterOffset) / RoadSpacing, MidpointRounding.AwayFromZero)
        * RoadSpacing;
}
