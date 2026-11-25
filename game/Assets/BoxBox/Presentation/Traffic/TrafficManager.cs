using System.Collections.Generic;
using BoxBox.Presentation.Vehicles;
using BoxBox.Roads;
using BoxBox.Roads.Traffic;
using UnityEngine;

namespace BoxBox.Presentation.Traffic
{
    /// <summary>
    /// Keeps a few civilian cars driving around the player, like civ_ai.c PingInCivCar /
    /// CheckPingOut: cars that drift too far away are taken out and put back on a lane nearer
    /// the player. Each car runs CivDriver (pure AI) and drives through VehiclePhysicsController.
    /// </summary>
    public sealed class TrafficManager : MonoBehaviour
    {
        private const float PingOutDistance = 230f, MinPingIn = 45f, MaxPingIn = 180f, MinGap = 15f;
        private static readonly Color[] Paints =
        {
            new Color(0.8f, 0.15f, 0.12f), new Color(0.15f, 0.3f, 0.75f), new Color(0.9f, 0.9f, 0.88f),
            new Color(0.12f, 0.12f, 0.13f), new Color(0.55f, 0.56f, 0.58f), new Color(0.2f, 0.5f, 0.25f),
            new Color(0.9f, 0.75f, 0.2f)
        };

        private sealed class CivCar
        {
            public VehiclePhysicsController Vehicle;
            public Rigidbody Body;
            public CivDriver Driver;
        }

        private readonly List<CivCar> _cars = new List<CivCar>();
        private RoadNetwork _network;
        private Transform _player;
        private VehicleSpec _spec;
        private System.Random _random;
        private int _seed;

        public int Count => _cars.Count;

        public void Init(RoadNetwork network, Transform player, int count, int seed)
        {
            _network = network;
            _player = player;
            _random = new System.Random(seed);
            _seed = seed;
            _spec = ScriptableObject.CreateInstance<VehicleSpec>();

            for (int i = 0; i < count; i++)
            {
                if (!TryFindSpawn(25f, MaxPingIn, out RoadSurfaceId road, out int lane, out float along)) continue;
                var car = new CivCar();
                car.Vehicle = VehicleFactory.Create(_spec, Vector3.zero, 0f, "Civ_" + i, out _);
                car.Body = car.Vehicle.GetComponent<Rigidbody>();
                VehicleFactory.Paint(car.Vehicle, Paints[i % Paints.Length]);
                Place(car, road, lane, along);
                _cars.Add(car);
            }
        }

        private void FixedUpdate()
        {
            if (_network == null) return;
            double time = Time.timeAsDouble;
            foreach (CivCar car in _cars)
            {
                Transform t = car.Vehicle.transform;
                if ((t.position - _player.position).sqrMagnitude > PingOutDistance * PingOutDistance || t.position.y < -10f)
                {
                    PingOut(car);
                    continue;
                }

                float speedMs = car.Vehicle.SpeedKmh / 3.6f;
                bool turning = Mathf.Abs(car.Vehicle.CurrentSteerAngle) > 61f * 360f / 4096f;   // wheel_angle < 61
                CivCommand cmd = car.Driver.Update(new RoadPoint(t.position.x, t.position.z), t.eulerAngles.y,
                    speedMs, ObstacleAhead(car), time, turning);
                car.Vehicle.SetAiControl(cmd.WheelAngleDeg, cmd.Thrust);
            }
        }

        // Nearest car (or the player) in this car's lane ahead, measured to its rear bumper.
        private float ObstacleAhead(CivCar car)
        {
            Transform me = car.Vehicle.transform;
            Vector3 fwd = me.forward; fwd.y = 0f; fwd.Normalize();
            var right = new Vector3(fwd.z, 0f, -fwd.x);
            float halfWidth = _spec.bodySize.x / 2f, halfLength = _spec.bodySize.z / 2f;
            float best = float.MaxValue;

            void Check(Vector3 other)
            {
                Vector3 rel = other - me.position;
                float tangent = Vector3.Dot(rel, fwd), normal = Vector3.Dot(rel, right);
                if (tangent > 0f && Mathf.Abs(normal) < halfWidth * 1.8f) best = Mathf.Min(best, tangent - halfLength);
            }

            foreach (CivCar other in _cars) if (other != car) Check(other.Vehicle.transform.position);
            Check(_player.position);
            return best;
        }

        private void PingOut(CivCar car)
        {
            if (TryFindSpawn(MinPingIn, MaxPingIn, out RoadSurfaceId road, out int lane, out float along))
                Place(car, road, lane, along);
        }

        private void Place(CivCar car, RoadSurfaceId road, int lane, float along)
        {
            RoadStraight s = _network.Straight(road);
            RoadPoint p = s.LaneCentre(lane, along);
            float heading = _network.LaneHeadingDeg(road, lane, p);
            car.Body.position = new Vector3(p.X, 0.5f, p.Z);
            car.Body.rotation = Quaternion.Euler(0f, heading, 0f);
            car.Vehicle.transform.SetPositionAndRotation(car.Body.position, car.Body.rotation);
            car.Body.linearVelocity = Vector3.zero;
            car.Body.angularVelocity = Vector3.zero;
            car.Driver = new CivDriver(_network, road, lane, along, _spec.bodySize.z / 2f, _seed++);
        }

        private bool TryFindSpawn(float minDist, float maxDist, out RoadSurfaceId road, out int lane, out float along)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                int index = _random.Next(_network.Straights.Count);
                RoadStraight s = _network.Straights[index];
                if (s.LengthM < 20f) continue;
                lane = _random.Next(s.Lanes.Count);
                if (!s.Lanes.IsAiLane(lane)) continue;
                along = ((float)_random.NextDouble() - 0.5f) * (s.LengthM - 16f);
                RoadPoint p = s.LaneCentre(lane, along);
                var world = new Vector3(p.X, 0f, p.Z);

                float toPlayer = Vector3.Distance(world, _player.position);
                if (toPlayer < minDist || toPlayer > maxDist) continue;
                if (!IsClear(world)) continue;

                road = new RoadSurfaceId(RoadSurfaceKind.Straight, index);
                return true;
            }
            road = RoadSurfaceId.None; lane = 0; along = 0f;
            return false;
        }

        private bool IsClear(Vector3 p)
        {
            foreach (CivCar car in _cars)
                if (car.Vehicle != null && (car.Vehicle.transform.position - p).sqrMagnitude < MinGap * MinGap) return false;
            return true;
        }
    }
}
