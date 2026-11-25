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

        private static readonly ICivSensor Empty = new FixedCivSensor();

        private static void AddArm(RoadNetwork net, RoadSurfaceId junction, RoadExit exit, float heading, RoadPoint mid)
        {
            RoadSurfaceId arm = net.Add(new RoadStraight(mid, 100f, heading, RoadLanes.TwoWay()));
            net.Link(arm, 1, junction, (int)exit);
        }

        [Test]
        public void LeftTurnWaitsForOncomingTrafficThenGoes()
        {
            // Four-way crossing; the car heads east and we pick a seed whose route turns left there.
            var town = new GridTown(new GridTownSpec { Columns = 2, Rows = 2, AvenueColumnLine = -1, AvenueRowLine = -1 });
            RoadNetwork net = town.Network;
            RoadSurfaceId west = net.Junction(town.JunctionAt(1, 1)).Exits[(int)RoadExit.West];
            RoadStraight s = net.Straight(west);
            CivDriver driver = null;
            for (int seed = 0; driver == null; seed++)
            {
                var candidate = new CivDriver(net, west, 0, 20f, 2.2f, seed);
                candidate.Path.TryNextStopLine(0f, out _, out _, out bool left);
                if (left) driver = candidate;
            }
            var oncoming = new FixedCivSensor { NearestMovingAhead = 8f };
            float x = s.Mid.X + 20f, z = s.LaneCentre(0, 0f).Z, v = 0f;
            double t = 512.0 / 30.0 * 3;   // start of a cycle: east/west green for 5 s
            int stoppedFrames = 0;
            for (int i = 0; i < 175; i++)   // 3.5 s, light green the whole time
            {
                CivCommand cmd = driver.Update(new RoadPoint(x, z), 90f, v, oncoming, t + i / 50.0, false);
                v = Math.Max(0f, v + cmd.Thrust * ThrustToMs2 / 50f);
                x += v / 50f;
                if (v < 0.3f && x > s.Mid.X + 21f) stoppedFrames++;
            }
            Assert.That(TrafficLights.PhaseAt(RoadExit.West, t + 3.5), Is.EqualTo(LightPhase.Green));
            Assert.That(stoppedFrames, Is.GreaterThan(20), "green, but waits for oncoming traffic");
            float waitX = x;

            oncoming.NearestMovingAhead = float.MaxValue;
            for (int i = 0; i < 50; i++)
            {
                CivCommand cmd = driver.Update(new RoadPoint(x, z), 90f, v, oncoming, t + 3.5 + i / 50.0, false);
                v = Math.Max(0f, v + cmd.Thrust * ThrustToMs2 / 50f);
                x += v / 50f;
            }
            Assert.That(x, Is.GreaterThan(waitX + 1f), "goes once clear");
        }

        [Test]
        public void ChangesLaneOnTheAvenueOnlyWhenTheSideIsFree()
        {
            var net = new RoadNetwork();
            RoadSurfaceId avenue = net.Add(new RoadStraight(new RoadPoint(0f, 0f), 2000f, 0f, RoadLanes.Avenue()));
            float Run(bool sideFree)
            {
                var driver = new CivDriver(net, avenue, 0, -900f, 2.2f, seed: 9);
                var sensor = new FixedCivSensor { SideFree = sideFree };
                RoadStraight s = net.Straight(avenue);
                float x = s.LaneCentre(0, 0f).X, z = -900f, v = 10f, h = 0f, maxX = 0f;
                for (int i = 0; i < 50 * 120; i++)
                {
                    CivCommand cmd = driver.Update(new RoadPoint(x, z), h, v, sensor, i / 50.0, false);
                    h += (float)(v / 2.7 * Math.Tan(cmd.WheelAngleDeg * Math.PI / 180) * 180 / Math.PI) / 50f;
                    x += v * (float)Math.Sin(h * Math.PI / 180) / 50f;
                    z += v * (float)Math.Cos(h * Math.PI / 180) / 50f;
                    maxX = Math.Max(maxX, Math.Abs(x - s.LaneCentre(0, 0f).X));
                }
                return maxX;
            }
            Assert.That(Run(false), Is.LessThan(1f), "blocked side: stays in lane");
            Assert.That(Run(true), Is.GreaterThan(2f), "free side: moves over a lane");
        }

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
            AddArm(net, junction, RoadExit.East, 90f, new RoadPoint(53.1f, 63.1f));
            AddArm(net, junction, RoadExit.West, 270f, new RoadPoint(-53.1f, 63.1f));

            var driver = new CivDriver(net, road, 0, 5f, 2.2f, seed: 1);
            RoadStraight s = net.Straight(road);
            float z = 5f, v = 0f, top = 0f;
            for (int i = 0; i < 420; i++)   // 8.4 s at 50 Hz; north is red until 8.53 s
            {
                CivCommand cmd = driver.Update(new RoadPoint(s.LaneCentre(0, 0f).X, z), 0f, v, Empty, i / 50.0, false);
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

            CivCommand free = driver.Update(new RoadPoint(x, -150f), 0f, 8f, Empty, 100.0, false);
            CivCommand blocked = driver.Update(new RoadPoint(x, -150f), 0f, 8f, new FixedCivSensor { ObstacleAhead = 3f }, 100.0, false);
            Assert.That(free.Thrust, Is.GreaterThan(0f));
            Assert.That(blocked.Thrust, Is.LessThan(0f));
            Assert.That(blocked.BrakeLight, Is.True);
        }
    }
}
