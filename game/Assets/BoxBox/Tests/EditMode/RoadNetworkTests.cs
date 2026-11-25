using BoxBox.Roads;
using NUnit.Framework;

namespace BoxBox.Tests
{
    public sealed class RoadNetworkTests
    {
        private const float W = RoadLanes.LaneWidthM;

        [Test]
        public void LaneWidthMatchesDriverScale()
        {
            Assert.That(W, Is.EqualTo(3.072f).Within(0.001f));
            Assert.That(RoadLanes.TwoWay().SpeedLimitKmh, Is.EqualTo(36f));
            Assert.That(RoadLanes.Avenue().SpeedLimitKmh, Is.EqualTo(63f));
        }

        [Test]
        public void TwoWayStreetDrivesOnTheRight()
        {
            var net = new RoadNetwork();
            RoadSurfaceId id = net.Add(new RoadStraight(new RoadPoint(0f, 0f), 50f, 0f, RoadLanes.TwoWay()));

            // Heading north: +X is the right-hand side = lane 0, which follows the heading.
            Assert.That(net.GetLane(id, new RoadPoint(1.5f, 0f)), Is.EqualTo(0));
            Assert.That(net.LaneHeadingDeg(id, 0, default), Is.EqualTo(0f));
            Assert.That(net.GetLane(id, new RoadPoint(-1.5f, 0f)), Is.EqualTo(1));
            Assert.That(net.LaneHeadingDeg(id, 1, default), Is.EqualTo(180f));
        }

        [Test]
        public void AvenueHasTwoLanesEachWayAndClampsOutsidePoints()
        {
            var net = new RoadNetwork();
            RoadSurfaceId id = net.Add(new RoadStraight(new RoadPoint(0f, 0f), 50f, 90f, RoadLanes.Avenue()));

            // Heading east: right-hand side is -Z.
            int[] expected = { 0, 1, 2, 3 };
            for (int lane = 0; lane < 4; lane++)
            {
                float z = -(2 - lane - 0.5f) * W;
                Assert.That(net.GetLane(id, new RoadPoint(0f, z)), Is.EqualTo(expected[lane]));
            }
            Assert.That(net.LaneHeadingDeg(id, 1, default), Is.EqualTo(90f));
            Assert.That(net.LaneHeadingDeg(id, 2, default), Is.EqualTo(270f));
            Assert.That(net.GetLane(id, new RoadPoint(0f, -40f)), Is.EqualTo(0));
            Assert.That(net.GetLane(id, new RoadPoint(0f, 40f)), Is.EqualTo(3));
        }

        [Test]
        public void CurveLaneCountsFromTheInsideAndFollowsTheTangent()
        {
            var net = new RoadNetwork();
            RoadSurfaceId id = net.Add(new RoadCurve(new RoadPoint(0f, 0f), 0f, 90f, 10f, RoadLanes.TwoWay()));

            var inner = new RoadPoint(0f, 10f + W * 0.5f);   // heading 0 from the centre
            Assert.That(net.FindSurface(inner), Is.EqualTo(id));
            Assert.That(net.GetLane(id, inner), Is.EqualTo(0));
            Assert.That(net.LaneHeadingDeg(id, 0, inner), Is.EqualTo(90f).Within(0.01f));
            Assert.That(net.GetLane(id, new RoadPoint(0f, 10f + W * 1.5f)), Is.EqualTo(1));
            Assert.That(net.FindSurface(new RoadPoint(-5f, 11f)), Is.EqualTo(RoadSurfaceId.None), "outside the arc span");
        }

        [Test]
        public void GridTownLinksEveryStraightToTwoJunctions()
        {
            var town = new GridTown(new GridTownSpec { Columns = 3, Rows = 2 });
            RoadNetwork net = town.Network;

            Assert.That(net.Junctions.Count, Is.EqualTo(4 * 3));
            Assert.That(net.Straights.Count, Is.EqualTo(4 * 2 + 3 * 3));
            for (int i = 0; i < net.Straights.Count; i++)
            {
                RoadStraight s = net.Straights[i];
                var self = new RoadSurfaceId(RoadSurfaceKind.Straight, i);
                foreach (int end in new[] { 0, 1 })
                {
                    RoadSurfaceId j = s.Connect[end];
                    Assert.That(j.Kind, Is.EqualTo(RoadSurfaceKind.Junction));
                    Assert.That(net.Slots(j), Has.Member(self), "links must go both ways");
                }
            }
        }

        [Test]
        public void GridTownPointsResolveToTheRightSurface()
        {
            var town = new GridTown(new GridTownSpec { Columns = 2, Rows = 2, AvenueColumnLine = 1, AvenueRowLine = -1 });
            RoadNetwork net = town.Network;
            RoadJunction centre = net.Junction(town.JunctionAt(1, 1));

            Assert.That(net.FindSurface(centre.Centre), Is.EqualTo(town.JunctionAt(1, 1)));
            Assert.That(centre.HalfSizeX * 2f, Is.EqualTo(4 * W).Within(0.001f), "avenue crossing is four lanes wide");

            // Halfway up the avenue, on its right-hand (east) side: northbound lane.
            var onAvenue = new RoadPoint(centre.Centre.X + W * 0.5f, centre.Centre.Z + 30f);
            RoadSurfaceId id = net.FindSurface(onAvenue);
            Assert.That(id.Kind, Is.EqualTo(RoadSurfaceKind.Straight));
            Assert.That(net.LaneHeadingDeg(id, net.GetLane(id, onAvenue), onAvenue), Is.EqualTo(0f));

            Assert.That(net.FindSurface(town.Lot(0, 0).Centre), Is.EqualTo(RoadSurfaceId.None));
            Assert.That(town.Lot(0, 0).SizeX, Is.EqualTo(60f - 6f).Within(0.001f));
        }
            [Test]
        public void RouteFinderCrossesTheTownAndAvoidsClosedStreets()
        {
            var town = new GridTown(new GridTownSpec { Columns = 3, Rows = 3 });
            var finder = new RouteFinder(town.Network);
            RoadSurfaceId start = town.JunctionAt(0, 0), goal = town.JunctionAt(3, 3);

            var route = finder.FindRoute(start, goal);
            Assert.That(route[0], Is.EqualTo(start));
            Assert.That(route[route.Count - 1], Is.EqualTo(goal));
            Assert.That(route.Count, Is.EqualTo(7), "six blocks on a grid, no detours");
            for (int i = 1; i < route.Count; i++)
                Assert.That(IsNeighbour(town.Network, route[i - 1], route[i]), Is.True);

            // Close every street leaving the start: unreachable.
            foreach (RoadSurfaceId exit in town.Network.Junction(start).Exits)
                if (exit.Kind == RoadSurfaceKind.Straight) finder.SetStraightOpen(exit, false);
            Assert.That(finder.FindRoute(start, goal), Is.Empty);

            finder.SetStraightOpen(town.Network.Junction(start).Exits[(int)RoadExit.East], true);
            route = finder.FindRoute(start, goal);
            Assert.That(route[1], Is.EqualTo(town.JunctionAt(1, 0)), "only the east street is open");
        }

        private static bool IsNeighbour(RoadNetwork net, RoadSurfaceId a, RoadSurfaceId b)
        {
            foreach (RoadSurfaceId exit in net.Junction(a).Exits)
                if (exit.Kind == RoadSurfaceKind.Straight && net.Slots(exit)[0].Equals(b) || exit.Kind == RoadSurfaceKind.Straight && net.Slots(exit)[1].Equals(b))
                    return true;
            return false;
        }
    }
}
