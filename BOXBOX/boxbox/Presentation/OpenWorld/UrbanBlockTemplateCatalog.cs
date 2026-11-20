namespace Boxbox.Presentation.OpenWorld;

public enum UrbanBlockTemplate
{
    CommercialStrip,
    MixedCourtyard,
    ResidentialCluster,
    PocketPark,
    MarketSquare,
    NeighborhoodGate
}

public static class UrbanBlockTemplateCatalog
{
    public static UrbanBlockTemplate Select(UrbanRegion region, int blockX, int blockZ, uint blockSeed)
    {
        if (blockX == region.AnchorBlockX && blockZ == region.AnchorBlockZ)
        {
            return region.District switch
            {
                DistrictStyle.Commercial => UrbanBlockTemplate.MarketSquare,
                DistrictStyle.Residential => UrbanBlockTemplate.NeighborhoodGate,
                DistrictStyle.Mixed => UrbanBlockTemplate.MarketSquare,
                _ => UrbanBlockTemplate.PocketPark
            };
        }

        return region.District switch
        {
            DistrictStyle.Commercial => blockSeed % 4 == 0
                ? UrbanBlockTemplate.MixedCourtyard
                : UrbanBlockTemplate.CommercialStrip,
            DistrictStyle.Residential => blockSeed % 5 == 0
                ? UrbanBlockTemplate.PocketPark
                : UrbanBlockTemplate.ResidentialCluster,
            DistrictStyle.Mixed => (blockSeed % 3) switch
            {
                0 => UrbanBlockTemplate.CommercialStrip,
                1 => UrbanBlockTemplate.ResidentialCluster,
                _ => UrbanBlockTemplate.MixedCourtyard
            },
            _ => blockSeed % 4 == 0
                ? UrbanBlockTemplate.NeighborhoodGate
                : UrbanBlockTemplate.PocketPark
        };
    }

    public static bool HasBuilding(UrbanBlockTemplate template, int localX, int localZ, uint cellSeed)
    {
        var nearEdge = WorldLayoutRules.RoadWidthCells;
        var farEdge = WorldLayoutRules.RoadPeriod - 1;
        var innerNear = nearEdge + 1;
        var innerFar = farEdge - 1;
        var edge = localX == nearEdge || localX == farEdge ||
                   localZ == nearEdge || localZ == farEdge;
        var corner = (localX == nearEdge || localX == farEdge) &&
                     (localZ == nearEdge || localZ == farEdge);
        var courtyardEdge = localX == innerNear || localX == innerFar ||
                            localZ == innerNear || localZ == innerFar;

        return template switch
        {
            UrbanBlockTemplate.CommercialStrip => edge && cellSeed % 6 != 0,
            UrbanBlockTemplate.MixedCourtyard => edge
                ? cellSeed % 3 != 0
                : courtyardEdge && cellSeed % 9 == 0,
            UrbanBlockTemplate.ResidentialCluster => corner || (edge && cellSeed % 3 == 0),
            UrbanBlockTemplate.MarketSquare => localZ == WorldLayoutRules.RoadPeriod - 1 && cellSeed % 4 != 0,
            UrbanBlockTemplate.NeighborhoodGate => corner,
            _ => false
        };
    }

    public static float NatureCluster(UrbanBlockTemplate template, int localX, int localZ, uint blockSeed)
    {
        var centerX = 2 + (int)(blockSeed % 4);
        var centerZ = 2 + (int)((blockSeed / 11) % 4);
        var distance = MathF.Sqrt((localX - centerX) * (localX - centerX) +
                                  (localZ - centerZ) * (localZ - centerZ));
        var cluster = Math.Clamp(1f - distance / 4.2f, 0f, 1f);
        return template switch
        {
            UrbanBlockTemplate.PocketPark => Math.Clamp(cluster + 0.38f, 0f, 1f),
            UrbanBlockTemplate.NeighborhoodGate => cluster * 0.7f,
            UrbanBlockTemplate.ResidentialCluster => cluster * 0.48f,
            UrbanBlockTemplate.MixedCourtyard => cluster * 0.35f,
            UrbanBlockTemplate.MarketSquare => cluster * 0.18f,
            _ => cluster * 0.12f
        };
    }
}
