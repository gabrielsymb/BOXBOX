using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    /// <summary>
    /// Engine and road resistance in SI units. Replaces Driver 2's constant thrust + linear drag,
    /// which gave 0-100 km/h in ~3.5 s and stopped the car almost at once when the throttle was released.
    ///
    /// Drive force = throttle * min(grip-limited launch force, wheel power / speed): strong pull when
    /// slow, fading with speed like a real engine through its gears. Resistance = air drag (v^2) +
    /// rolling resistance + engine braking, so the car coasts and keeps momentum.
    /// </summary>
    public sealed class VehicleEngine
    {
        private const float AirDensity = 1.2f;
        private const float Gravity = 9.81f;

        private float _throttle;

        public float Throttle => _throttle;

        /// <summary>Throttle pedal goes down gradually, so a held key still builds up.</summary>
        public void UpdatePedal(VehicleSpec spec, float input, float dt)
        {
            float target = Mathf.Max(0f, input);
            float rate = target > _throttle ? 1f / Mathf.Max(0.01f, spec.throttleRiseSeconds) : 4f;
            _throttle = Mathf.MoveTowards(_throttle, target, rate * dt);
        }

        /// <summary>Total force from the driven wheels, in newtons.</summary>
        public float DriveForce(VehicleSpec spec, float speedMs)
        {
            float powerLimited = spec.wheelPowerKw * 1000f / Mathf.Max(Mathf.Abs(speedMs), 0.5f);
            return _throttle * Mathf.Min(spec.maxDriveForceN, powerLimited);
        }

        /// <summary>
        /// Reverse gear pull in newtons (negative = backwards). Strong at standstill, fading to
        /// zero at spec.maxReverseKmh, so reversing has a real top speed.
        /// </summary>
        public float ReverseForce(VehicleSpec spec, float speedMs, float input)
        {
            float backwardsMs = Mathf.Max(0f, -speedMs);
            float fade = Mathf.Clamp01(1f - backwardsMs / Mathf.Max(1f, spec.maxReverseKmh / 3.6f));
            return -Mathf.Clamp01(-input) * spec.maxDriveForceN * 0.6f * fade;
        }

        /// <summary>Force opposing forward motion (air + rolling + engine braking), in newtons, >= 0.</summary>
        public float ResistanceForce(VehicleSpec spec, float speedMs, float massKg)
        {
            float v = Mathf.Abs(speedMs);
            float air = 0.5f * AirDensity * spec.dragArea * v * v;
            float lowSpeedFade = Mathf.Clamp01(v / 0.5f);   // no creeping backwards when stopped
            float rolling = spec.rollingResistance * massKg * Gravity * lowSpeedFade;
            float engineBrake = (1f - _throttle) * spec.engineBrakeDecel * massKg * lowSpeedFade;
            return air + rolling + engineBrake;
        }
    }
}
