using System;

namespace BoxBox.Roads
{
    /// <summary>Axis-aligned rectangle on the ground, in metres.</summary>
    public readonly struct GroundRect
    {
        public float MinX { get; }
        public float MinZ { get; }
        public float MaxX { get; }
        public float MaxZ { get; }
        public GroundRect(float minX, float minZ, float maxX, float maxZ) { MinX = minX; MinZ = minZ; MaxX = maxX; MaxZ = maxZ; }
        public float SizeX => MaxX - MinX;
        public float SizeZ => MaxZ - MinZ;
        public RoadPoint Centre => new RoadPoint((MinX + MaxX) / 2f, (MinZ + MaxZ) / 2f);
        public GroundRect Inset(float m) => new GroundRect(MinX + m, MinZ + m, MaxX - m, MaxZ - m);
    }

    /// <summary>Parameters of a Manhattan-style greybox town (the GTA2 / Driver city-block idea).</summary>
    public sealed class GridTownSpec
    {
        public int Columns = 4;
        public int Rows = 4;
        /// <summary>Distance between the kerbs of two parallel streets (lot + both sidewalks).</summary>
        public float BlockM = 60f;
        public float SidewalkM = 3f;
        /// <summary>Street line (0..Columns) that is a four-lane avenue; -1 = none.</summary>
        public int AvenueColumnLine = 2;
        /// <summary>Street line (0..Rows) that is a four-lane avenue; -1 = none.</summary>
        public int AvenueRowLine = 2;
        public float OriginX;
        public float OriginZ;
    }

    /// <summary>
    /// Builds the road graph of a grid town: one junction per crossing, one straight between each
    /// pair of neighbouring junctions (north-south streets head 0°, east-west streets head 90°).
    /// Pure data; the Unity side only draws it.
    /// </summary>
    public sealed class GridTown
    {
        private readonly float[] _lineX, _lineZ;
        private readonly RoadSurfaceId[,] _junctions;

        public GridTownSpec Spec { get; }
        public RoadNetwork Network { get; } = new RoadNetwork();

        public GridTown(GridTownSpec spec)
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            if (spec.Columns < 1 || spec.Rows < 1) throw new ArgumentException("Town needs at least one block.");
            if (spec.SidewalkM * 2f >= spec.BlockM) throw new ArgumentException("Sidewalks wider than the block.");

            _lineX = LinePositions(spec.Columns, spec.OriginX, i => ColumnLanes(i).WidthM);
            _lineZ = LinePositions(spec.Rows, spec.OriginZ, j => RowLanes(j).WidthM);
            _junctions = new RoadSurfaceId[spec.Columns + 1, spec.Rows + 1];

            for (int i = 0; i <= spec.Columns; i++)
                for (int j = 0; j <= spec.Rows; j++)
                    _junctions[i, j] = Network.Add(new RoadJunction(new RoadPoint(_lineX[i], _lineZ[j]),
                        ColumnLanes(i).WidthM, RowLanes(j).WidthM));

            for (int i = 0; i <= spec.Columns; i++)
                for (int j = 0; j < spec.Rows; j++)
                    AddStraight(_junctions[i, j], _junctions[i, j + 1], 0f, ColumnLanes(i),
                        (int)RoadExit.South, (int)RoadExit.North);

            for (int j = 0; j <= spec.Rows; j++)
                for (int i = 0; i < spec.Columns; i++)
                    AddStraight(_junctions[i, j], _junctions[i + 1, j], 90f, RowLanes(j),
                        (int)RoadExit.West, (int)RoadExit.East);
        }

        public RoadSurfaceId JunctionAt(int line, int row) => _junctions[line, row];

        /// <summary>Area between four streets, sidewalks included.</summary>
        public GroundRect Block(int column, int row) => new GroundRect(
            _lineX[column] + ColumnLanes(column).WidthM / 2f, _lineZ[row] + RowLanes(row).WidthM / 2f,
            _lineX[column + 1] - ColumnLanes(column + 1).WidthM / 2f, _lineZ[row + 1] - RowLanes(row + 1).WidthM / 2f);

        /// <summary>Buildable lot inside the sidewalks.</summary>
        public GroundRect Lot(int column, int row) => Block(column, row).Inset(Spec.SidewalkM);

        private RoadLanes ColumnLanes(int line) => line == Spec.AvenueColumnLine ? RoadLanes.Avenue() : RoadLanes.TwoWay();
        private RoadLanes RowLanes(int line) => line == Spec.AvenueRowLine ? RoadLanes.Avenue() : RoadLanes.TwoWay();

        // from = back end, to = forward end of the new straight.
        private void AddStraight(RoadSurfaceId from, RoadSurfaceId to, float heading, RoadLanes lanes,
            int exitOfTo, int exitOfFrom)
        {
            RoadJunction a = Network.Junction(from), b = Network.Junction(to);
            bool northSouth = heading == 0f;
            float start = northSouth ? a.Centre.Z + a.HalfSizeZ : a.Centre.X + a.HalfSizeX;
            float end = northSouth ? b.Centre.Z - b.HalfSizeZ : b.Centre.X - b.HalfSizeX;
            float mid = (start + end) / 2f;
            var centre = northSouth ? new RoadPoint(a.Centre.X, mid) : new RoadPoint(mid, a.Centre.Z);

            RoadSurfaceId id = Network.Add(new RoadStraight(centre, end - start, heading, lanes));
            Network.Link(id, 0, to, exitOfTo);
            Network.Link(id, 1, from, exitOfFrom);
        }

        private float[] LinePositions(int blocks, float origin, Func<int, float> width)
        {
            var lines = new float[blocks + 1];
            lines[0] = origin;
            for (int i = 1; i <= blocks; i++)
                lines[i] = lines[i - 1] + width(i - 1) / 2f + Spec.BlockM + width(i) / 2f;
            return lines;
        }
    }
}
