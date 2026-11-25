using System;
using System.Collections.Generic;

namespace BoxBox.Roads
{
    /// <summary>
    /// The road graph: straights, curves and junctions linked through their Connect/Exits ids.
    /// Queries are ported from REDRIVER2 dr2roads.c (GetSurfaceRoadInfo, GetLaneByPositionOnRoad)
    /// and civ_ai.c (lane heading). Driver finds the surface with a per-cell BSP; a linear scan is
    /// enough for a greybox town and keeps this independent of any engine.
    /// </summary>
    public sealed class RoadNetwork
    {
        private readonly List<RoadStraight> _straights = new List<RoadStraight>();
        private readonly List<RoadCurve> _curves = new List<RoadCurve>();
        private readonly List<RoadJunction> _junctions = new List<RoadJunction>();

        public IReadOnlyList<RoadStraight> Straights => _straights;
        public IReadOnlyList<RoadCurve> Curves => _curves;
        public IReadOnlyList<RoadJunction> Junctions => _junctions;

        public RoadSurfaceId Add(RoadStraight s) { _straights.Add(s); return new RoadSurfaceId(RoadSurfaceKind.Straight, _straights.Count - 1); }
        public RoadSurfaceId Add(RoadCurve c) { _curves.Add(c); return new RoadSurfaceId(RoadSurfaceKind.Curve, _curves.Count - 1); }
        public RoadSurfaceId Add(RoadJunction j) { _junctions.Add(j); return new RoadSurfaceId(RoadSurfaceKind.Junction, _junctions.Count - 1); }

        public RoadStraight Straight(RoadSurfaceId id) => id.Kind == RoadSurfaceKind.Straight ? _straights[id.Index] : null;
        public RoadCurve Curve(RoadSurfaceId id) => id.Kind == RoadSurfaceKind.Curve ? _curves[id.Index] : null;
        public RoadJunction Junction(RoadSurfaceId id) => id.Kind == RoadSurfaceKind.Junction ? _junctions[id.Index] : null;

        /// <summary>The surface under a point. Junctions win over the road ends they overlap.</summary>
        public RoadSurfaceId FindSurface(RoadPoint p)
        {
            for (int i = 0; i < _junctions.Count; i++)
                if (_junctions[i].Contains(p)) return new RoadSurfaceId(RoadSurfaceKind.Junction, i);
            for (int i = 0; i < _straights.Count; i++)
                if (_straights[i].Contains(p)) return new RoadSurfaceId(RoadSurfaceKind.Straight, i);
            for (int i = 0; i < _curves.Count; i++)
                if (_curves[i].Contains(p)) return new RoadSurfaceId(RoadSurfaceKind.Curve, i);
            return RoadSurfaceId.None;
        }

        /// <summary>Lane layout of a road piece; false for junctions and off-road (GetSurfaceRoadInfo).</summary>
        public bool TryGetLanes(RoadSurfaceId id, out RoadLanes lanes)
        {
            lanes = default;
            if (id.Kind == RoadSurfaceKind.Straight) { lanes = _straights[id.Index].Lanes; return true; }
            if (id.Kind == RoadSurfaceKind.Curve) { lanes = _curves[id.Index].Lanes; return true; }
            return false;
        }

        /// <summary>GetLaneByPositionOnRoad: lane index under the point, clamped; -1 if not a road.</summary>
        public int GetLane(RoadSurfaceId id, RoadPoint p)
        {
            int lane;
            int count;
            if (id.Kind == RoadSurfaceKind.Straight)
            {
                RoadStraight s = _straights[id.Index];
                count = s.Lanes.Count;
                // lane = NumLanes - ((dx·cos − dz·sin) + 512 >> 9): 512 U is one whole lane, so floor(x + 1).
                lane = s.Lanes.HalfCount - (int)Math.Floor(s.Across(p) / RoadLanes.LaneWidthM + 1f);
            }
            else if (id.Kind == RoadSurfaceKind.Curve)
            {
                RoadCurve c = _curves[id.Index];
                count = c.Lanes.Count;
                // lane = (dist >> 9) − inside·2
                lane = (int)Math.Floor((c.Radius(p) - c.InsideRadiusM) / RoadLanes.LaneWidthM);
            }
            else return -1;

            if (lane < 0) lane = 0;
            if (lane >= count) lane = count - 1;
            return lane;
        }

        /// <summary>Heading a vehicle must drive in that lane at that point (civ_ai.c, cr->dir).</summary>
        public float LaneHeadingDeg(RoadSurfaceId id, int lane, RoadPoint p)
        {
            if (id.Kind == RoadSurfaceKind.Straight)
            {
                RoadStraight s = _straights[id.Index];
                return Normalize(s.Lanes.IsReversed(lane) ? s.HeadingDeg + 180f : s.HeadingDeg);
            }
            if (id.Kind == RoadSurfaceKind.Curve)
            {
                RoadCurve c = _curves[id.Index];
                float a = c.AngleOf(p);
                return Normalize(c.Lanes.IsReversed(lane) ? a - 90f : a + 90f);
            }
            throw new ArgumentException("Junctions have no lanes.", nameof(id));
        }

        /// <summary>Links two pieces both ways through the given slot of each.</summary>
        public void Link(RoadSurfaceId a, int slotA, RoadSurfaceId b, int slotB)
        {
            Slots(a)[slotA] = b;
            Slots(b)[slotB] = a;
        }

        public RoadSurfaceId[] Slots(RoadSurfaceId id)
        {
            switch (id.Kind)
            {
                case RoadSurfaceKind.Straight: return _straights[id.Index].Connect;
                case RoadSurfaceKind.Curve: return _curves[id.Index].Connect;
                case RoadSurfaceKind.Junction: return _junctions[id.Index].Exits;
                default: throw new ArgumentException("No surface.", nameof(id));
            }
        }

        public static float Normalize(float deg) { deg %= 360f; return deg < 0f ? deg + 360f : deg; }
    }
}
