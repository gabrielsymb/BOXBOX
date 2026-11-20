namespace Boxbox.Presentation.OpenWorld;

public readonly record struct UrbanRegion(
    int IdX,
    int IdZ,
    int AnchorBlockX,
    int AnchorBlockZ,
    DistrictStyle District);

public static class UrbanRegionPlanner
{
    private const int Seed = 26011988;
    private const int RegionSpanBlocks = 5;

    public static UrbanRegion RegionAt(int blockX, int blockZ)
    {
        var macroX = FloorDiv(blockX, RegionSpanBlocks);
        var macroZ = FloorDiv(blockZ, RegionSpanBlocks);
        var bestDistance = long.MaxValue;
        var best = default(UrbanRegion);

        for (var candidateX = macroX - 1; candidateX <= macroX + 1; candidateX++)
        for (var candidateZ = macroZ - 1; candidateZ <= macroZ + 1; candidateZ++)
        {
            var hash = Hash(candidateX, candidateZ, 701);
            var jitterX = (int)(hash % 3) - 1;
            var jitterZ = (int)((hash / 7) % 3) - 1;
            var anchorX = candidateX * RegionSpanBlocks + RegionSpanBlocks / 2 + jitterX;
            var anchorZ = candidateZ * RegionSpanBlocks + RegionSpanBlocks / 2 + jitterZ;
            var deltaX = blockX - anchorX;
            var deltaZ = blockZ - anchorZ;
            var distance = (long)deltaX * deltaX + (long)deltaZ * deltaZ;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = new UrbanRegion(
                candidateX,
                candidateZ,
                anchorX,
                anchorZ,
                (DistrictStyle)(Hash(candidateX, candidateZ, 977) % 4));
        }

        return best;
    }

    public static uint BlockSeed(int blockX, int blockZ) => Hash(blockX, blockZ, 1201);

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
