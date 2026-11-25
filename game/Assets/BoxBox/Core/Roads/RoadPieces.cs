using System;

namespace BoxBox.Roads
{
    /// <summary>Point on the ground plane, in metres (Unity x and z).</summary>
    public readonly struct RoadPoint
    {
        public float X { get; }
        public float Z { get; }
        public RoadPoint(float x, float z) { X = x; Z = z; }
        public static RoadPoint operator +(RoadPoint a, RoadPoint b) => new RoadPoint(a.X + b.X, a.Z + b.Z);
        public static RoadPoint operator *(RoadPoint a, float s) => new RoadPoint(a.X * s, a.Z * s);

        /// <summary>Unit vector of a Driver-style heading: 0° = +Z, 90° = +X.</summary>
        public static RoadPoint FromHeading(float degrees)
        {
            double r = degrees * Math.PI / 180.0;
            return new RoadPoint((float)Math.Sin(r), (float)Math.Cos(r));
        }
    }

    public enum RoadSurfaceKind { None, Straight, Curve, Junction }

    /// <summary>Typed version of Driver's surfId (type in the high bits, index in the low 13).</summary>
    public readonly struct RoadSurfaceId : IEquatable<RoadSurfaceId>
    {
        public static readonly RoadSurfaceId None = default;
        public RoadSurfaceKind Kind { get; }
        public int Index { get; }
        public RoadSurfaceId(RoadSurfaceKind kind, int index) { Kind = kind; Index = index; }
        public bool IsRoad => Kind == RoadSurfaceKind.Straight || Kind == RoadSurfaceKind.Curve;
        public bool Equals(RoadSurfaceId other) => Kind == other.Kind && Index == other.Index;
        public override bool Equals(object obj) => obj is RoadSurfaceId other && Equals(other);
        public override int GetHashCode() => ((int)Kind << 16) ^ Index;
        public override string ToString() => Kind == RoadSurfaceKind.None ? "None" : $"{Kind}#{Index}";
    }

    /// <summary>DRIVER2_STRAIGHT: centre, length and heading. Connect[0] = forward end, [1] = back end.</summary>
    public sealed class RoadStraight
    {
        public RoadPoint Mid { get; }
        public float LengthM { get; }
        public float HeadingDeg { get; }
        public RoadLanes Lanes { get; }
        public RoadSurfaceId[] Connect { get; } = new RoadSurfaceId[4];

        public RoadStraight(RoadPoint mid, float lengthM, float headingDeg, RoadLanes lanes)
        {
            Mid = mid; LengthM = lengthM; HeadingDeg = headingDeg; Lanes = lanes;
        }

        public RoadPoint Direction => RoadPoint.FromHeading(HeadingDeg);
        /// <summary>Unit vector towards lane 0 (right-hand side of the heading).</summary>
        public RoadPoint Right => RoadPoint.FromHeading(HeadingDeg + 90f);

        public float Along(RoadPoint p) { RoadPoint d = Direction; return (p.X - Mid.X) * d.X + (p.Z - Mid.Z) * d.Z; }
        public float Across(RoadPoint p) { RoadPoint r = Right; return (p.X - Mid.X) * r.X + (p.Z - Mid.Z) * r.Z; }

        public bool Contains(RoadPoint p) =>
            Math.Abs(Along(p)) <= LengthM / 2f && Math.Abs(Across(p)) <= Lanes.WidthM / 2f;

        /// <summary>Centre of a lane at a distance along the piece (0 = Mid).</summary>
        public RoadPoint LaneCentre(int lane, float along) =>
            Mid + Direction * along + Right * ((Lanes.HalfCount - lane - 0.5f) * RoadLanes.LaneWidthM);
    }

    /// <summary>
    /// DRIVER2_CURVE: arc around Mid from heading StartDeg to EndDeg (increasing, clockwise seen from
    /// above). InsideRadiusM is the inner edge (Driver: inside × 1024 U); lane 0 is the inner lane.
    /// </summary>
    public sealed class RoadCurve
    {
        public RoadPoint Mid { get; }
        public float StartDeg { get; }
        public float EndDeg { get; }
        public float InsideRadiusM { get; }
        public RoadLanes Lanes { get; }
        public RoadSurfaceId[] Connect { get; } = new RoadSurfaceId[4];

        public RoadCurve(RoadPoint mid, float startDeg, float endDeg, float insideRadiusM, RoadLanes lanes)
        {
            Mid = mid; StartDeg = startDeg; EndDeg = endDeg; InsideRadiusM = insideRadiusM; Lanes = lanes;
        }

        public float Radius(RoadPoint p) =>
            (float)Math.Sqrt((p.X - Mid.X) * (p.X - Mid.X) + (p.Z - Mid.Z) * (p.Z - Mid.Z));

        /// <summary>ratan2(dx, dz) in degrees, 0..360.</summary>
        public float AngleOf(RoadPoint p)
        {
            float a = (float)(Math.Atan2(p.X - Mid.X, p.Z - Mid.Z) * 180.0 / Math.PI);
            return a < 0f ? a + 360f : a;
        }

        public bool Contains(RoadPoint p)
        {
            float r = Radius(p);
            if (r < InsideRadiusM || r > InsideRadiusM + Lanes.WidthM) return false;
            float span = Wrap(EndDeg - StartDeg);
            return Wrap(AngleOf(p) - StartDeg) <= span;
        }

        private static float Wrap(float deg) { deg %= 360f; return deg < 0f ? deg + 360f : deg; }
    }

    /// <summary>
    /// DRIVER2_JUNCTION: up to four exits (index = RoadExit). Driver takes the junction area from the
    /// collision surface; here it is an axis-aligned rectangle.
    /// </summary>
    public sealed class RoadJunction
    {
        public RoadPoint Centre { get; }
        public float HalfSizeX { get; }
        public float HalfSizeZ { get; }
        public RoadSurfaceId[] Exits { get; } = new RoadSurfaceId[4];

        public RoadJunction(RoadPoint centre, float sizeX, float sizeZ)
        {
            Centre = centre; HalfSizeX = sizeX / 2f; HalfSizeZ = sizeZ / 2f;
        }

        public bool Contains(RoadPoint p) =>
            Math.Abs(p.X - Centre.X) <= HalfSizeX && Math.Abs(p.Z - Centre.Z) <= HalfSizeZ;
    }

    public enum RoadExit { North = 0, East = 1, South = 2, West = 3 }
}
