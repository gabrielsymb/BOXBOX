using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    /// <summary>
    /// Per-vehicle characteristics, the equivalent of Driver 2's CAR_COSMETICS + HANDLING_TYPE.
    /// Create one asset per vehicle: Assets > Create > BOXBOX > Vehicle Spec.
    /// See research/redriver2-vehicle-handling-study-2026-11-24.md for what each value does.
    /// </summary>
    [CreateAssetMenu(menuName = "BOXBOX/Vehicle Spec", fileName = "VehicleSpec")]
    public sealed class VehicleSpec : ScriptableObject
    {
        [Header("Mass")]
        [Tooltip("Heavier = slower acceleration and lower top speed for the same engine, and pushes harder in collisions. " +
                 "Grip (cornering/braking g) does not change with mass, like real tyres.")]
        public float massKg = 1350f;
        [Tooltip("Centre of mass relative to the car origin (origin = ground level under the car at rest).")]
        public Vector3 centerOfMass = new Vector3(0f, 0.55f, 0f);

        [Header("Engine")]
        [Tooltip("Power that reaches the wheels (about 80% of the brochure engine power). Sets acceleration at speed and top speed.")]
        public float wheelPowerKw = 65f;
        [Tooltip("Max push of the driven wheels when launching (first gear / grip limit). 8000 N on 1350 kg = 0.6 g.")]
        public float maxDriveForceN = 8000f;
        [Tooltip("Seconds for the throttle to go from 0 to full while W is held. Higher = gentler build-up.")]
        public float throttleRiseSeconds = 0.8f;
        [Tooltip("Brake strength (CAR_COSMETICS.powerRatio / 4096 in Driver).")]
        public float powerRatio = 1f;
        [Tooltip("Top speed in reverse gear. Real cars: ~20-30 km/h.")]
        public float maxReverseKmh = 22f;

        [Header("Resistance (momentum)")]
        [Tooltip("Drag coefficient x frontal area (m2). Car ~0.7, pickup ~1.1, truck ~5. Sets top speed.")]
        public float dragArea = 0.7f;
        [Tooltip("Tyre rolling resistance coefficient. Asphalt ~0.012, heavy tyres ~0.015.")]
        public float rollingResistance = 0.012f;
        [Tooltip("Deceleration from the engine when the throttle is released (m/s2). Low = long coasting.")]
        public float engineBrakeDecel = 0.3f;

        [Header("Grip")]
        [Tooltip("CAR_COSMETICS.traction / 4096. Scales grip on both axles.")]
        public float traction = 1f;
        [Tooltip("HANDLING_TYPE.frictionScaleRatio / 32. Driver uses 1.0 for player cars, up to 2.1 for chase cars.")]
        public float frictionScaleRatio = 1f;
        [Tooltip("Height at which tyre forces act, as a fraction of the real lever (wheelforces.c:497). " +
                 "1 = full lever (rolls easily); Driver cop cars use 19/32 = 0.59 or 12/32 = 0.38.")]
        [Range(0.2f, 1f)] public float tyreForceHeight = 19f / 32f;

        [Header("Steering")]
        [Tooltip("Max wheel angle when stopped (A/D).")]
        public float maxSteerDegrees = 31f;
        [Tooltip("Max wheel angle at highSteerSpeedKmh and above (A/D). Lower = smoother high-speed curves, less rollover.")]
        public float highSpeedSteerDegrees = 10f;
        public float highSteerSpeedKmh = 110f;
        [Tooltip("How fast the wheel turns while A/D is held. Lower = taps give gentler curves.")]
        public float steerRateDegPerSec = 40f;
        [Tooltip("Shift + A/D: sharp turn. Driver's fast steer reached 45 deg at ~170 deg/s.")]
        public float fastSteerDegrees = 45f;
        public float fastSteerRateDegPerSec = 170f;
        [Tooltip("How fast the wheel returns to centre when released.")]
        public float steerCentreRateDegPerSec = 170f;

        [Header("Body and wheels")]
        public Vector3 bodySize = new Vector3(1.85f, 1.0f, 4.4f);
        [Tooltip("Gap between ground and the bottom of the body collider at rest.")]
        public float groundClearance = 0.4f;
        public float wheelRadius = 0.35f;
        [Tooltip("Distance between left and right tyre contact points.")]
        public float track = 1.7f;
        [Tooltip("Distance between front and rear axles.")]
        public float wheelbase = 2.7f;

        [Header("Cargo bed (pickup)")]
        [Tooltip("Length of the open cargo bed at the back. 0 = closed car body, no bed.")]
        public float bedLength = 0f;
        [Tooltip("Height of the bed side walls and tailgate above the bed floor.")]
        public float bedWallHeight = 0.45f;
        [Tooltip("Height of the chassis slab; its top is the bed floor.")]
        public float chassisHeight = 0.5f;
    }
}
