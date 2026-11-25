using BoxBox.Roads;
using UnityEngine;

namespace BoxBox.Presentation.World
{
    /// <summary>
    /// The SandboxLab town: a 4×4 grid with a four-lane avenue each way. Placed so the grocery's
    /// parking pad is at the world origin (where the vehicle spawns), the test yard is the empty lot
    /// across the street, and the wholesaler is three blocks away on the far side of the avenues.
    /// </summary>
    public sealed class SandboxTown
    {
        private const int StoreColumn = 0, StoreRow = 1;
        private const int YardColumn = 1, YardRow = 1;
        private const int WholesalerColumn = 3, WholesalerRow = 3;

        public GridTown Town { get; }
        public Vector3 StorePad { get; }
        public Vector3 WholesalerPad { get; }
        public Vector3 TestYardCentre { get; }

        public SandboxTown()
        {
            // Grocery pad hugs the east edge of its lot so it opens onto the street (building behind it).
            var spec = new GridTownSpec();
            RoadPoint pad = StorePadIn(new GridTown(spec));
            spec.OriginX = -pad.X;
            spec.OriginZ = -pad.Z;
            Town = new GridTown(spec);

            StorePad = ToWorld(StorePadIn(Town));
            GroundRect wholesale = Town.Lot(WholesalerColumn, WholesalerRow);
            WholesalerPad = new Vector3(wholesale.MinX + 7.5f, 0f, wholesale.Centre.Z);
            TestYardCentre = ToWorld(Town.Lot(YardColumn, YardRow).Centre);
        }

        public Transform Build()
        {
            GroundRect yard = Town.Lot(YardColumn, YardRow);
            var driveways = new[]
            {
                // grocery opens east, wholesaler west (where their pads are); the yard on every side
                new TownBuilder.Driveway(StoreColumn, StoreRow, RoadExit.East, StorePad.z, 16f),
                new TownBuilder.Driveway(WholesalerColumn, WholesalerRow, RoadExit.West, WholesalerPad.z, 18f),
                new TownBuilder.Driveway(YardColumn, YardRow, RoadExit.North, yard.Centre.X, 14f),
                new TownBuilder.Driveway(YardColumn, YardRow, RoadExit.South, yard.Centre.X, 14f),
                new TownBuilder.Driveway(YardColumn, YardRow, RoadExit.East, yard.Centre.Z, 14f),
                new TownBuilder.Driveway(YardColumn, YardRow, RoadExit.West, yard.Centre.Z, 14f),
            };
            Transform root = TownBuilder.Build(Town, IsBuildable, seed: 2026, driveways);
            root.gameObject.AddComponent<TrafficLightView>().Build(Town.Network);
            return root;
        }

        private static bool IsBuildable(int c, int r) =>
            !(c == StoreColumn && r == StoreRow) && !(c == YardColumn && r == YardRow) &&
            !(c == WholesalerColumn && r == WholesalerRow);

        private static RoadPoint StorePadIn(GridTown town)
        {
            GroundRect lot = town.Lot(StoreColumn, StoreRow);
            return new RoadPoint(lot.MaxX - 6.5f, lot.Centre.Z);
        }

        private static Vector3 ToWorld(RoadPoint p) => new Vector3(p.X, 0f, p.Z);
    }
}
