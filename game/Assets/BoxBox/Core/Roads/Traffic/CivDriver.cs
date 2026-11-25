using System;

namespace BoxBox.Roads.Traffic
{
    /// <summary>Per-step command for a civilian car: wheel angle and Driver thrust.</summary>
    public readonly struct CivCommand
    {
        public float WheelAngleDeg { get; }
        /// <summary>Driver thrust units per rear wheel; negative while rolling forward = brake.</summary>
        public float Thrust { get; }
        public bool BrakeLight { get; }
        public CivCommand(float wheelAngleDeg, float thrust, bool brakeLight)
        {
            WheelAngleDeg = wheelAngleDeg; Thrust = thrust; BrakeLight = brakeLight;
        }
    }

    /// <summary>
    /// Civilian driver ported from REDRIVER2 civ_ai.c (CivControl, CivSteerAngle, CivAccel,
    /// CivAccelTrafficRules, SetUpCivCollFlags), converted to metres with 1 U = 0.006 m:
    /// - steering aims at a point (speed × 20 frames + half body) ahead on the path, max 45°;
    /// - thrust is newAccel = 2000, cut by 3 per steering unit; braking never pushes a stopped car;
    /// - a stop line with a red or amber light is approached at a speed proportional to the distance
    ///   left (velRatio), stopping 3 half-bodies short, and the car waits for green;
    /// - a car or the player inside the look-ahead box (half body + 0.56 m + speed × 13 frames, or
    ///   × 4 while turning) makes it brake.
    /// Adaptation: Driver limits top speed with a linear drag we no longer have, so above the road
    /// limit the driver lifts off instead of giving a quarter of the thrust.
    /// </summary>
    public sealed class CivDriver
    {
        public const float NewAccel = 2000f;
        private const float MaxSteerDeg = 45f;          // maxSteer = 512 of 4096
        private const float CheckSeconds = 20f / 30f;   // checkFrames = 20
        private const float VdPerMs = 4096f / (0.006f * 30f);
        private const float AngleUnitsPerDeg = 4096f / 360f;
        private const int NodesAhead = 8;

        private enum Thrust { Accelerate, StopAtNode, Stopped }

        private readonly CivPath _path = new CivPath();
        private readonly CivRouteBuilder _route;
        private readonly float _halfBody;
        private Thrust _state = Thrust.Accelerate;
        private float _velRatio;

        public CivPath Path => _path;

        public CivDriver(RoadNetwork network, RoadSurfaceId straight, int lane, float along, float halfBodyM, int seed)
        {
            _halfBody = halfBodyM;
            _route = new CivRouteBuilder(network, seed);
            _route.Start(_path, straight, lane, along);
            Refill();
        }

        public CivCommand Update(RoadPoint position, float headingDeg, float speedMs, float obstacleAheadM, double timeSeconds, bool turning)
        {
            Refill();
            float station = _path.FindStation(position);

            // CivSteerAngle
            RoadPoint target = _path.PointOnPath(station + Math.Max(0f, speedMs) * CheckSeconds + _halfBody);
            float aim = (float)(Math.Atan2(target.X - position.X, target.Z - position.Z) * 180.0 / Math.PI);
            float steer = Clamp(DeltaAngle(headingDeg, aim), -MaxSteerDeg, MaxSteerDeg);

            float thrust = TrafficRules(station, speedMs, timeSeconds, out bool brakeLight);

            // SetUpCivCollFlags + CivAccel: something in the box ahead -> brake by speed, up to 2 × newAccel
            float box = _halfBody + 0.56f + Math.Abs(speedMs) / 30f * (turning ? 4f : 13f);
            if (obstacleAheadM < box)
            {
                thrust = Math.Min(thrust, Math.Max(-speedMs * VdPerMs / 4f, -2f * NewAccel));
                brakeLight = true;
            }

            // CivControl: less thrust while steering; never brake a car that has stopped
            if (thrust > 0f) thrust -= Math.Max(Math.Abs(steer) * AngleUnitsPerDeg, 4f) * 3f;
            if (thrust < 0f && speedMs * VdPerMs < 100f) thrust = 0f;

            return new CivCommand(steer, thrust, brakeLight);
        }

        private float TrafficRules(float station, float speedMs, double time, out bool brakeLight)
        {
            brakeLight = false;
            float limitMs = _path.MinLimitWithin(station, 20f, 36f) / 3.6f;
            float stopDist = _halfBody * 3f;
            bool hasStop = _path.TryNextStopLine(station, out CivNode stop, out float distToNode);
            bool mustStop = hasStop && TrafficLights.PhaseAt(stop.Approach, time) != LightPhase.Green;

            switch (_state)
            {
                case Thrust.Accelerate:
                    if (mustStop)
                    {
                        float a = NewAccel * 2f * 0.006f * 30f * 30f / 4096f * 1.11f;   // newAccel as m/s²
                        float brakeDist = speedMs * speedMs / (2f * a);
                        // too close to stop before the line (light just changed): carry on through it
                        if (brakeDist > distToNode - stopDist && distToNode > stopDist && brakeDist < distToNode + 2f)
                        {
                            // properVel = wheel_speed - 120000 Vd, spread over the distance left
                            _velRatio = (speedMs - 120000f / VdPerMs) / Math.Max(1f, distToNode - stopDist);
                            // Adaptation: a slow car would get a negative ratio and stall short of the line.
                            _velRatio = Math.Max(_velRatio, 0.6f);
                            _state = Thrust.StopAtNode;
                            break;
                        }
                    }
                    return speedMs > limitMs ? 0f : NewAccel;
                case Thrust.Stopped:
                    brakeLight = true;
                    if (!mustStop) { _state = Thrust.Accelerate; return NewAccel; }
                    return Math.Max(-speedMs * VdPerMs / 4f, -2f * NewAccel);
            }

            // CIV_AI_THRUST_STOP_AT_NODE
            brakeLight = true;
            if (!mustStop) { _state = Thrust.Accelerate; return NewAccel; }
            if (distToNode < stopDist + 0.5f) { _state = Thrust.Stopped; return -speedMs * VdPerMs / 4f; }
            // properVel = velRatio × (distToNode − distToEnd + 100 U); accel = (properVel − v) × newAccel / 15
            float properVel = _velRatio * (distToNode - stopDist + 100f * 0.006f);
            float accel = (properVel - speedMs) * VdPerMs * NewAccel / 15f;
            return Clamp(accel, -2f * NewAccel, NewAccel);
        }

        private void Refill()
        {
            int guard = 0;
            while (_path.Count < NodesAhead && guard++ < 4) _route.Extend(_path);
        }

        private static float DeltaAngle(float from, float to)
        {
            float d = (to - from) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
