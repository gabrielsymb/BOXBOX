using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum DistrictStyle { Commercial, Residential, Mixed, Green }

public readonly record struct WorldCellPlan(
    bool IsRoad, bool IsIntersection, bool IsCrosswalk, bool IsRamp,
    bool HasBuilding, bool HasCombatTarget, DistrictStyle District,
    int Variant, float FacingYaw, uint DetailSeed,
    UrbanBlockTemplate BlockTemplate, bool IsLandmark, float NatureCluster);

public static class WorldLayoutRules
{
    public const int RoadPeriod = 8;
    public const int RoadWidthCells = 2;
    private const int Seed = 26011988;

    public static WorldCellPlan Plan(Vector2I coordinate)
    {
        var localX = FloorMod(coordinate.X, RoadPeriod);
        var localZ = FloorMod(coordinate.Y, RoadPeriod);
        var blockX = FloorDiv(coordinate.X, RoadPeriod);
        var blockZ = FloorDiv(coordinate.Y, RoadPeriod);
        var region = UrbanRegionPlanner.RegionAt(blockX, blockZ);
        var blockSeed = UrbanRegionPlanner.BlockSeed(blockX, blockZ);
        var template = UrbanBlockTemplateCatalog.Select(region, blockX, blockZ, blockSeed);
        var detailSeed = Hash(coordinate.X, coordinate.Y, 43);
        var landmark = blockX == region.AnchorBlockX && blockZ == region.AnchorBlockZ;

        var verticalRoad = localX < RoadWidthCells;
        var horizontalRoad = localZ < RoadWidthCells;
        var intersectionAnchor = localX == 0 && localZ == 0;
        var crosswalk =
            (localX == 0 && (localZ == RoadWidthCells || localZ == RoadPeriod - 1)) ||
            (localZ == 0 && (localX == RoadWidthCells || localX == RoadPeriod - 1));
        if (verticalRoad || horizontalRoad)
            return new(true, intersectionAnchor, crosswalk, false, false, false,
                region.District, 0, 0, detailSeed, template, landmark, 0);

        var ramp = localZ == RoadWidthCells && localX == RoadPeriod / 2;
        var hasBuilding = !ramp && UrbanBlockTemplateCatalog.HasBuilding(
            template, localX, localZ, detailSeed);
        var variant = region.District switch
        {
            DistrictStyle.Commercial => (int)(detailSeed % 2),
            DistrictStyle.Residential => 2 + (int)(detailSeed % 2),
            DistrictStyle.Green => 2 + (int)(detailSeed % 2),
            _ => (int)(detailSeed % 4)
        };
        var natureCluster = UrbanBlockTemplateCatalog.NatureCluster(template, localX, localZ, blockSeed);

        return new(false, false, false, ramp, hasBuilding,
            !hasBuilding && !ramp && detailSeed % 31 == 0,
            region.District, variant, FacingYaw(localX, localZ), detailSeed,
            template, landmark, natureCluster);
    }

    private static float FacingYaw(int localX, int localZ)
    {
        var north = localZ - (RoadWidthCells - 1);
        var south = RoadPeriod - localZ;
        var west = localX - (RoadWidthCells - 1);
        var east = RoadPeriod - localX;
        var nearest = Math.Min(Math.Min(north, south), Math.Min(west, east));
        if (nearest == north) return 0;
        if (nearest == east) return Mathf.Pi / 2f;
        if (nearest == south) return Mathf.Pi;
        return -Mathf.Pi / 2f;
    }

    private static uint Hash(int x, int z, int salt)
    {
        unchecked
        {
            var hash = Seed ^ salt;
            hash = (hash * 397) ^ x;
            hash = (hash * 397) ^ z;
            hash ^= hash >> 16;
            return (uint)hash;
        }
    }

    private static int FloorMod(int value, int divisor) => ((value % divisor) + divisor) % divisor;
    private static int FloorDiv(int value, int divisor) => (value - FloorMod(value, divisor)) / divisor;
}
