using System;
using BoxBox.Roads;
using BoxBox.Roads.Traffic;
using NUnit.Framework;

namespace BoxBox.Tests
{
    public sealed class TrafficTests
    {
        // Thrust per rear wheel -> m/s² for a 1350 kg car (2 wheels × AccUnitToMs2 × tarmac 1.11).
        private const float ThrustToMs2 = 2f * 0.006f * 30f * 30f / 4096f * 1.11f;

        [Test]
        public void LightsAlternateBetweenAxesLikeDriver()
        {
            Assert.That(TrafficLights.Phase(RoadExit.East, 0), Is.EqualTo(LightPhase.Green));
            Assert.That(TrafficLights.Phase(RoadExit.North, 0), Is.EqualTo(LightPhase.Red));
            Assert.That(TrafficLights.Phase(RoadExit.East, 170), Is.EqualTo(LightPhase.Amber));
            Assert.That(TrafficLights.Phase(RoadExit.East, 300), Is.EqualTo(LightPhase.Red));
            Assert.That(TrafficLights.Phase(RoadExit.South, 300), Is.EqualTo(LightPhase.Green));
            for (long f = 0; f < 512; f++)
                Assert.That(TrafficLights.Phase(RoadExit.North, f) == LightPhase.Green &&
                            TrafficLights.Phase(RoadExit.West, f) == LightPhase.Green, Is.False, "never both green");
        }

        [Test]
        public void RouteStaysOnRightHandLanesAndNeverUTurns()
        {
            var town = new GridTown(new GridTownSpec { Columns = 3, Rows = 3 });
            RoadNetwork net = town.Network;
            for (int seed = 0; seed < 20; seed++)
            {
                var path = new CivPath();
                var route = new CivRouteBuilder(net, seed);
                route.Start(path, new RoadSurfaceId(RoadSurfaceKind.Straight, 0), 0, 0f);
                RoadSurfaceId previous = route.Road;
                for (int step = 0; step < 12; step++)
                {
                    route.Extend(path);
                    Assert.That(route.Road, Is.Not.EqualTo(previous), "no U-turn");
                    RoadStraight s = net.Straight(route.Road);
                    RoadPoint end = path.Nodes[path.Count - 1].Pos;
                    // The lane end must be the far end in the lane's driving direction.
                    float heading = net.LaneHeadingDeg(route.Road, route.Lane, end);
                    RoadPoint dir = RoadPoint.FromHeading(heading);
                    Assert.That((end.X - s.Mid.X) * dir.X + (end.Z - s.Mid.Z) * dir.Z, Is.GreaterThan(0f));
                    Assert.That(path.Nodes[path.Count - 1].IsStopLine, Is.True);
                    previous = route.Road;
                }
            }
        }

        [Test]
        public void CarReachesTheSpeedLimitAndStopsAtARedLight()
        {
            // One straight heading north into a junction; north/south is red for t < 8.5 s.
            var net = new RoadNetwork();
            RoadSurfaceId road = net.Add(new RoadStraight(new RoadPoint(0f, 0f), 120f, 0f, RoadLanes.TwoWay()));
            RoadSurfaceId junction = net.Add(new RoadJunction(new RoadPoint(0f, 63.1f), 6.2f, 6.2f));
            net.Link(road, 0, junction, (int)RoadExit.South);

            var driver = new CivDriver(net, road, 0, 5f, 2.2f, seed: 1);
            RoadStraight s = net.Straight(road);
            float z = 5f, v = 0f, top = 0f;
            for (int i = 0; i < 420; i++)   // 8.4 s at 50 Hz; north is red until 8.53 s
            {
                CivCommand cmd = driver.Update(new RoadPoint(s.LaneCentre(0, 0f).X, z), 0f, v, float.MaxValue, i / 50.0, false);
                v = Math.Max(0f, v + cmd.Thrust * ThrustToMs2 / 50f);
                z += v / 50f;
                top = Math.Max(top, v);
            }
            Assert.That(top * 3.6f, Is.GreaterThan(25f), "got going");
            Assert.That(top * 3.6f, Is.LessThan(45f), "respects the 36 km/h street");
            Assert.That(v, Is.LessThan(0.5f), "waiting at the light");
            Assert.That(z, Is.LessThan(60f), "stopped before the stop line");
            Assert.That(z, Is.GreaterThan(45f), "but close to it");
        }

        [Test]
        public void CarBrakesForTheCarAhead()
        {
            var net = new RoadNetwork();
            RoadSurfaceId road = net.Add(new RoadStraight(new RoadPoint(0f, 0f), 400f, 0f, RoadLanes.TwoWay()));
            var driver = new CivDriver(net, road, 0, -150f, 2.2f, seed: 1);
            float x = net.Straight(road).LaneCentre(0, 0f).X;

            CivCommand free = driver.Update(new RoadPoint(x, -150f), 0f, 8f, float.MaxValue, 100.0, false);
            CivCommand blocked = driver.Update(new RoadPoint(x, -150f), 0f, 8f, 3f, 100.0, false);
            Assert.That(free.Thrust, Is.GreaterThan(0f));
            Assert.That(blocked.Thrust, Is.LessThan(0f));
            Assert.That(blocked.BrakeLight, Is.True);
        }
    }
}
