using System;
using System.Collections.Generic;

namespace BoxBox.Roads.Traffic
{
    /// <summary>
    /// Extends a civilian car's path one road at a time, the job of civ_ai.c CreateNewNode and
    /// GetNextRoadInfo: follow the lane to the end of the straight, put a stop line there, pick a
    /// random exit of the junction (never a U-turn), turn into the matching lane of the next straight.
    /// Right turns take the rightmost lane, left turns the leftmost, straight on keeps the lane.
    /// </summary>
    public sealed class CivRouteBuilder
    {
        private readonly RoadNetwork _network;
        private readonly Random _random;

        public RoadSurfaceId Road { get; private set; }
        public int Lane { get; private set; }

        public CivRouteBuilder(RoadNetwork network, int seed)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _random = new Random(seed);
        }

        /// <summary>Starts on a lane at a distance along the straight (0 = middle).</summary>
        public void Start(CivPath path, RoadSurfaceId straight, int lane, float along)
        {
            RoadStraight s = _network.Straight(straight) ?? throw new ArgumentException("Not a straight.");
            Road = straight;
            Lane = lane;
            path.Add(new CivNode(s.LaneCentre(lane, along), s.Lanes.SpeedLimitKmh));
            AddLaneEnd(path, s, lane);
        }

        /// <summary>Adds the next junction crossing and the following lane.</summary>
        public void Extend(CivPath path)
        {
            RoadStraight current = _network.Straight(Road);
            bool forward = !current.Lanes.IsReversed(Lane);
            RoadSurfaceId junctionId = current.Connect[forward ? 0 : 1];
            RoadJunction junction = _network.Junction(junctionId);
            if (junction == null) return;

            int approach = Array.IndexOf(junction.Exits, Road);
            var options = new List<int>();
            for (int e = 0; e < 4; e++)
                if (e != approach && junction.Exits[e].Kind == RoadSurfaceKind.Straight) options.Add(e);
            if (options.Count == 0) options.Add(approach);   // dead end: Driver turns around too

            RoadSurfaceId nextId = junction.Exits[options[_random.Next(options.Count)]];
            RoadStraight next = _network.Straight(nextId);
            bool nextForward = next.Connect[1].Equals(junctionId);   // leaving the junction from the back end

            float inHeading = Heading(current, forward), outHeading = Heading(next, nextForward);
            float turn = Delta(inHeading, outHeading);
            int rank = RankOf(current.Lanes, Lane, forward);
            int count = LanesGoing(next.Lanes, nextForward);
            int nextRank = turn > 45f ? 0 : turn < -45f ? count - 1 : Math.Min(rank, count - 1);
            int nextLane = LaneAtRank(next.Lanes, nextRank, nextForward);

            RoadPoint from = path.Nodes[path.Count - 1].Pos;
            RoadPoint to = next.LaneCentre(nextLane, nextForward ? -next.LengthM / 2f : next.LengthM / 2f);
            if (Math.Abs(turn) > 45f && TryIntersect(from, inHeading, to, outHeading, out RoadPoint corner))
            {
                for (int i = 1; i <= 3; i++)   // quadratic curve through the corner
                {
                    float t = i / 4f, u = 1f - t;
                    path.Add(new CivNode(new RoadPoint(
                        u * u * from.X + 2f * u * t * corner.X + t * t * to.X,
                        u * u * from.Z + 2f * u * t * corner.Z + t * t * to.Z), next.Lanes.SpeedLimitKmh * 0.5f));
                }
            }
            path.Add(new CivNode(to, next.Lanes.SpeedLimitKmh));
            Road = nextId;
            Lane = nextLane;
            AddLaneEnd(path, next, nextLane);
        }

        private void AddLaneEnd(CivPath path, RoadStraight s, int lane)
        {
            bool forward = !s.Lanes.IsReversed(lane);
            RoadSurfaceId junctionId = s.Connect[forward ? 0 : 1];
            RoadJunction junction = _network.Junction(junctionId);
            var approach = junction != null ? (RoadExit)Array.IndexOf(junction.Exits, Road) : RoadExit.North;
            path.Add(new CivNode(s.LaneCentre(lane, forward ? s.LengthM / 2f : -s.LengthM / 2f),
                s.Lanes.SpeedLimitKmh, isStopLine: junction != null, approach: approach));
        }

        private static float Heading(RoadStraight s, bool forward) => RoadNetwork.Normalize(s.HeadingDeg + (forward ? 0f : 180f));

        private static float Delta(float from, float to)
        {
            float d = (to - from) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }

        // Rank 0 = rightmost lane in the direction of travel.
        private static int RankOf(RoadLanes lanes, int lane, bool forward)
        {
            int rank = 0;
            for (int k = 0; k < lanes.Count; k++)
            {
                int candidate = forward ? k : lanes.Count - 1 - k;
                if (lanes.IsReversed(candidate) == !forward)
                {
                    if (candidate == lane) return rank;
                    rank++;
                }
            }
            return 0;
        }

        private static int LanesGoing(RoadLanes lanes, bool forward)
        {
            int n = 0;
            for (int k = 0; k < lanes.Count; k++) if (lanes.IsReversed(k) == !forward) n++;
            return Math.Max(1, n);
        }

        private static int LaneAtRank(RoadLanes lanes, int rank, bool forward)
        {
            for (int k = 0; k < lanes.Count; k++)
            {
                int candidate = forward ? k : lanes.Count - 1 - k;
                if (lanes.IsReversed(candidate) == !forward && rank-- == 0) return candidate;
            }
            return forward ? 0 : lanes.Count - 1;
        }

        private static bool TryIntersect(RoadPoint a, float headingA, RoadPoint b, float headingB, out RoadPoint hit)
        {
            RoadPoint da = RoadPoint.FromHeading(headingA), db = RoadPoint.FromHeading(headingB);
            float denom = da.X * db.Z - da.Z * db.X;
            hit = default;
            if (Math.Abs(denom) < 1e-4f) return false;
            float t = ((b.X - a.X) * db.Z - (b.Z - a.Z) * db.X) / denom;
            hit = new RoadPoint(a.X + da.X * t, a.Z + da.Z * t);
            return true;
        }
    }
}
