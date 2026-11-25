// Handling model ported from REDRIVER2 (MIT License, Copyright (c) 2020 SoapyMan):
//   research/third-party/redriver2/src_rebuild/Game/C/wheelforces.c
//     GetFrictionScalesDriver1, AddWheelForcesDriver1, StepOneCar
//   research/third-party/redriver2/src_rebuild/Game/C/handling.c
//     ProcessCarPad (steering + thrust), GlobalTimeStep (integration)
// Study, unit derivation and differences: research/redriver2-vehicle-handling-study-2026-11-24.md
//
// The original runs in 12-bit fixed point at 30 steps/s with its own rigid body integrator.
// Here every quantity is computed in the original units ("D units") so the formulas stay
// readable side by side with the C source, then converted to SI and handed to a Unity Rigidbody.

using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehiclePhysicsController : MonoBehaviour
    {
        [System.Serializable]
        public struct Wheel
        {
            public string Name;
            /// <summary>Body-fixed tyre contact point at static ride height (Driver's wheelDisp).</summary>
            public Transform MountPoint;
            public Transform VisualMesh;
            public bool IsFront;
            [HideInInspector] public bool IsGrounded;
            [HideInInspector] public float Compression;      // D units (0.6 cm), 0..42, rest ~14
            [HideInInspector] public float SpinDegrees;
        }

        // ---------- Unit conversion (see study doc, section "Unidades") ----------
        private const float StepRate = 30f;                       // Driver 2 simulates once per 30 Hz frame
        private const float StepTime = 1f / StepRate;
        // Calibrated so GRAVITY_FORCE equals 9.81 m/s^2 (7456/4096 units/step^2 at 30 Hz -> 0.006 m/unit).
        // Cross-check: civ_ai.c speedLimits {56,97,138} units/step -> 36/63/89 km/h.
        private const float MetersPerUnit = 0.006f;
        private const float VelUnitToMs = MetersPerUnit * StepRate / 4096f;   // 1 Vd -> m/s
        private const float AccUnitToMs2 = VelUnitToMs * StepRate;            // 1 Vd/step -> m/s^2
        private const float AngleUnitToDeg = 360f / 4096f;

        // ---------- Constants copied from the C source ----------
        private const float GravityForce = 7456f;                 // wheelforces.c GRAVITY_FORCE
        private const float RestCompression = 14f;
        private const float MaxCompression = 42f;
        private const float LockSpeed = 41943f;                    // wheel_speed above which braking locks wheels
        private const float FrictionTotal = 0x780;                 // rearFS = 0x780 - frontFS

        // Driver's thrust is an acceleration shared by every car. Here it becomes an engine force sized
        // for a 1350 kg car, so a heavier vehicle with the same engine accelerates (and tops out) slower.
        public const float ReferenceMassKg = 1350f;

        [Header("Car characteristics")]
        [SerializeField] private VehicleSpec spec;

        // ConvertTorqueToAngularAcceleration damps avel by 128/4096 per step (~0.95 /s)
        private const float AngularDamping = 0.95f;

        [Header("Wheels")]
        [SerializeField] private Wheel[] wheels;

        public void SetWheels(Wheel[] wheelArray) => wheels = wheelArray;

        public VehicleSpec Spec => spec;

        public void SetSpec(VehicleSpec vehicleSpec)
        {
            spec = vehicleSpec;
            if (_rb != null) ApplySpec();
        }


        private Rigidbody _rb;
        private float _throttleInput;
        private float _steerInput;
        private bool _handbrake;
        private bool _fastSteer;

        // CAR_DATA state
        private float _wheelAngle;   // D angle units, + = right
        private float _thrust;       // Vd per step
        private readonly VehicleEngine _engine = new VehicleEngine();
        private float _wheelSpeed;   // forward speed, Vd
        private float _autoBrake;    // steps steering has been held
        private bool _frontLocked, _rearLocked;

        public float SpeedKmh => _rb != null ? Vector3.Dot(_rb.linearVelocity, transform.forward) * 3.6f : 0f;
        public float CurrentSteerAngle => _wheelAngle * AngleUnitToDeg;
        public Rigidbody Body => _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (spec == null) spec = ScriptableObject.CreateInstance<VehicleSpec>();
            _rb.useGravity = false;                 // Driver gravity applied in FixedUpdate (= 9.8 m/s^2 at this scale)
            _rb.linearDamping = 0f;                 // drag comes from the wheels, as in the original
            _rb.angularDamping = AngularDamping;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Start() => ApplySpec();

        private void ApplySpec()
        {
            _rb.mass = spec.massKg;
            // Set after colliders exist so Unity's inertia tensor comes from the body box.
            _rb.ResetInertiaTensor();
            _rb.centerOfMass = spec.centerOfMass;
        }

        public void SetInputs(float throttle, float steer, bool handbrake, bool fastSteer = false)
        {
            _fastSteer = fastSteer;
            _throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            _steerInput = Mathf.Clamp(steer, -1f, 1f);
            _handbrake = handbrake;
        }

        private void FixedUpdate()
        {
            if (wheels == null || wheels.Length == 0) return;

            float dt = Time.fixedDeltaTime;
            float steps = dt / StepTime;    // fraction of a Driver step covered by this tick

            _rb.AddForce(Vector3.down * GravityForce * AccUnitToMs2, ForceMode.Acceleration);

            ProcessPad(steps);
            AddWheelForces(steps);
            AddRoadResistance();
        }

        private float ForwardSpeedMs() => Vector3.Dot(_rb.linearVelocity, transform.forward);

        // Air drag + rolling resistance + engine braking, applied at the centre of mass while on the ground.
        private void AddRoadResistance()
        {
            bool grounded = false;
            foreach (Wheel w in wheels) grounded |= w.IsGrounded;
            if (!grounded) return;

            Vector3 flat = Vector3.ProjectOnPlane(_rb.linearVelocity, Vector3.up);
            if (flat.sqrMagnitude < 1e-4f) return;
            float resist = _engine.ResistanceForce(spec, flat.magnitude, _rb.mass);
            _rb.AddForce(-flat.normalized * resist, ForceMode.Force);
        }

        // handling.c ProcessCarPad
        private void ProcessPad(float steps)
        {
            bool steering = Mathf.Abs(_steerInput) > 0.1f;

            if (steering)
            {
                float speed01 = Mathf.Clamp01(Mathf.Abs(SpeedKmh) / spec.highSteerSpeedKmh);
                float maxDeg = _fastSteer ? spec.fastSteerDegrees : Mathf.Lerp(spec.maxSteerDegrees, spec.highSpeedSteerDegrees, speed01);
                float rateDeg = _fastSteer ? spec.fastSteerRateDegPerSec : spec.steerRateDegPerSec;
                float maxUnits = maxDeg / AngleUnitToDeg;
                float target = Mathf.Sign(_steerInput) * maxUnits;
                _wheelAngle = Mathf.MoveTowards(_wheelAngle, target, rateDeg / AngleUnitToDeg * steps * StepTime);
                _autoBrake = Mathf.Min(_autoBrake + steps, 90f);
            }
            else
            {
                _wheelAngle = Mathf.MoveTowards(_wheelAngle, 0f, spec.steerCentreRateDegPerSec / AngleUnitToDeg * steps * StepTime);
                _autoBrake = 0f;
            }

            _thrust = 0f;
            _engine.UpdatePedal(spec, _throttleInput, steps * StepTime);
            if (_throttleInput < -0.05f && ForwardSpeedMs() > 0.5f)
            {
                // Driver brake: full brake force while still rolling forward
                _thrust = spec.powerRatio * -5000f;
            }
            else if (_throttleInput < -0.05f)
            {
                // reverse gear (VehicleEngine): Driver's taper relied on the linear drag removed with the engine model
                float perWheelN = _engine.ReverseForce(spec, ForwardSpeedMs(), _throttleInput) / 2f;
                _thrust = perWheelN / (AccUnitToMs2 * ReferenceMassKg);
            }
            else if (_throttleInput > 0.05f)
            {
                // VehicleEngine replaces Driver's constant 4915 thrust; converted to Driver units per rear wheel
                float perWheelN = _engine.DriveForce(spec, ForwardSpeedMs()) / 2f;
                _thrust = Mathf.Max(1f, perWheelN / (AccUnitToMs2 * ReferenceMassKg));
            }
        }

        // wheelforces.c GetFrictionScalesDriver1 (player handling type 0: autoBrakeOn, aggressive braking)
        private void GetFrictionScales(out float frontFS, out float rearFS)
        {
            frontFS = _thrust < 0f ? 1453f : (_thrust < 1f ? 937f : 820f);

            // holding the steering gradually gives the front more bite (max after 13 steps)
            if (_autoBrake > 0f && _wheelSpeed > 0f)
                frontFS += Mathf.Min(_autoBrake, 13f) * 3f * 15f;

            bool brakeLock = _thrust < 0f && _wheelSpeed > LockSpeed;
            _frontLocked = brakeLock;
            _rearLocked = brakeLock;

            if (_handbrake)
            {
                if (_thrust > -1f) _thrust = 0f;
                frontFS += _wheelSpeed < 1f ? -375f : 656f;
                _rearLocked = true;
            }

            if (_wheelSpeed < 0f && _thrust > -1f && !_handbrake)
                frontFS -= 400f;

            rearFS = FrictionTotal - frontFS;

            if (brakeLock)
            {
                frontFS = frontFS * 10f / 8f;
                rearFS = rearFS * 10f / 8f;
            }
            else
            {
                frontFS = frontFS * 35f / 32f;   // (36 - 1)/32 on tarmac
            }

            frontFS *= spec.frictionScaleRatio * spec.traction;
            rearFS *= spec.frictionScaleRatio * spec.traction;
        }

        // wheelforces.c AddWheelForcesDriver1
        private void AddWheelForces(float steps)
        {
            Vector3 velMs = _rb.linearVelocity;
            Vector3 vel = velMs / VelUnitToMs;                       // Vd

            // hd.speed: cheap |v| estimate in units/step
            float a = Mathf.Abs(vel.x / 4096f), b = Mathf.Abs(vel.z / 4096f);
            float speed = a < b ? b + a / 2f : a + b / 2f;

            // grip gain vs speed: ~0.8 when stopped, ~0.34 above ~14 km/h, ~0.27 at top speed
            float oldSpeed = speed * 1.5f;
            oldSpeed = oldSpeed < 32f ? oldSpeed * -72f + 3696f : 1424f - oldSpeed;
            oldSpeed = Mathf.Min(oldSpeed, 3300f);

            Vector3 fwd = transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
            Vector3 steerFwd = Quaternion.AngleAxis(_wheelAngle * AngleUnitToDeg, Vector3.up) * fwd;
            Vector3 steerRight = new Vector3(steerFwd.z, 0f, -steerFwd.x);

            GetFrictionScales(out float frontFS, out float rearFS);

            const float frictionCoefDry = (4096f * 32400f / 32768f + 500f) / 4096f;   // ~1.11

            // tilted car loses grip (m[1][1] < 2048 -> car more than 60 deg from upright)
            float tiltScale = 1f;
            float up = transform.up.y;
            if (up < 0.5f)
            {
                float t = 1f - up;
                tiltScale = t <= 1f ? 1f - t * t : 0f;
            }

            bool rearGrounded = false;
            float comY = _rb.worldCenterOfMass.y;

            for (int i = 0; i < wheels.Length; i++)
            {
                ref Wheel wheel = ref wheels[i];
                if (wheel.MountPoint == null) continue;

                Vector3 wheelPos = wheel.MountPoint.position;
                float oldCompression = wheel.Compression;
                float newCompression = 0f;
                Vector3 normal = Vector3.up;

                if (FindSurface(wheelPos, out RaycastHit hit))
                {
                    normal = hit.normal;
                    newCompression = (hit.point.y - wheelPos.y) / MetersPerUnit * normal.y + RestCompression;
                    if (newCompression < 0f) newCompression = 0f;
                    if (newCompression > 800f) newCompression = 12f;
                    if (newCompression > MaxCompression) newCompression = MaxCompression;
                }

                wheel.IsGrounded = newCompression > 0f;
                wheel.Compression = newCompression;
                if (newCompression == 0f && oldCompression == 0f) continue;
                if (!wheel.IsFront) rearGrounded = true;

                Vector3 pointVel = _rb.GetPointVelocity(wheelPos) / VelUnitToMs;
                pointVel.y = 0f;

                // spring 230*new - 100*old == 130*new + 100*(new - old); the rate term is rescaled
                // because Unity's tick is not the 30 Hz step the constants were tuned for
                float susForce = 130f * newCompression + 100f * (newCompression - oldCompression) / steps;

                bool locked = wheel.IsFront ? _frontLocked : _rearLocked;
                Vector3 lf;
                if (locked)
                {
                    // locked tyre: friction acts against the direction the contact patch is sliding
                    lf = pointVel.sqrMagnitude > 1e-6f ? pointVel.normalized : fwd;
                    if (Mathf.Abs(pointVel.x) + Mathf.Abs(pointVel.z) < 8000f) normal = Vector3.up;
                }
                else
                {
                    lf = wheel.IsFront ? steerRight : right;
                }

                float slidevel = Vector3.Dot(pointVel, lf);
                if (slidevel > 50000f) slidevel = 12500f;
                else if (slidevel < -50000f) slidevel = -12500f;
                else slidevel = Mathf.Clamp(oldSpeed * slidevel / 4096f, -12500f, 12500f);  // saturation = the slide

                Vector3 force = Vector3.zero;   // Vd per step
                float sidevel;

                if (!wheel.IsFront)
                {
                    if (!locked)
                    {
                        sidevel = rearFS * slidevel / 4096f;
                        if (sidevel * _wheelAngle > 0f) _autoBrake = -1f;   // counter-steering: drop the turn-in boost
                        force += fwd * (_thrust * ReferenceMassKg / _rb.mass);  // rear-wheel drive: engine force / mass
                    }
                    else
                    {
                        sidevel = frontFS * slidevel / 16384f;
                    }
                }
                else
                {
                    sidevel = frontFS * slidevel / 4096f;
                    if (locked)
                    {
                        sidevel = (frontFS * slidevel / 8192f + sidevel) / 2f;
                        // locked fronts still push sideways along the steered axle, so you can steer a lock-up
                        float forcefac = -2f * sidevel * Vector3.Dot(lf, steerRight);
                        force += steerRight * forcefac;
                    }
                }

                force += new Vector3(normal.x, 0f, normal.z) * susForce - lf * sidevel;   // Driver's linear drag replaced by AddRoadResistance

                float frictionCoef = frictionCoefDry * tiltScale;
                if (normal.y < 0.8f) frictionCoef *= normal.y * 5f / 4f;   // steep surface

                force.x *= frictionCoef;
                force.z *= frictionCoef;
                force.y = susForce * normal.y - vel.y * (12f / 4096f);

                // wheelforces.c:497 shortens the vertical lever (pursuer cars) so cornering force rolls the body less
                Vector3 applyPos = wheelPos;
                applyPos.y = comY + (wheelPos.y - comY) * spec.tyreForceHeight;
                _rb.AddForceAtPosition(force * AccUnitToMs2 * _rb.mass, applyPos, ForceMode.Force);
            }

            _wheelSpeed = rearGrounded
                ? Vector3.Dot(fwd, new Vector3(vel.x, 0f, vel.z))
                : (_thrust >= 1f ? 1703936f + 0x4000 : (_thrust <= -1f ? -1245184f + 0x4000 : 0f));
        }

        private bool FindSurface(Vector3 wheelPos, out RaycastHit best)
        {
            const float above = 0.6f, below = 0.6f;
            RaycastHit[] hits = Physics.RaycastAll(wheelPos + Vector3.up * above, Vector3.down, above + below, ~0, QueryTriggerInteraction.Ignore);
            best = default;
            bool found = false;
            for (int h = 0; h < hits.Length; h++)
            {
                if (hits[h].collider.attachedRigidbody == _rb) continue;
                if (!found || hits[h].distance < best.distance) { best = hits[h]; found = true; }
            }
            return found;
        }

        private void Update()
        {
            if (wheels == null) return;
            float forwardMs = Vector3.Dot(_rb.linearVelocity, transform.forward);

            for (int i = 0; i < wheels.Length; i++)
            {
                ref Wheel wheel = ref wheels[i];
                if (wheel.VisualMesh == null || wheel.MountPoint == null) continue;

                // cars.c: wheel centre = contact point + wheelSize, shifted by (compression - 14)
                float lift = spec.wheelRadius + (wheel.Compression - RestCompression) * MetersPerUnit;
                wheel.VisualMesh.position = wheel.MountPoint.position + transform.up * lift;

                bool locked = wheel.IsFront ? _frontLocked : _rearLocked;
                if (!locked) wheel.SpinDegrees += forwardMs / spec.wheelRadius * Mathf.Rad2Deg * Time.deltaTime;

                float steer = wheel.IsFront ? CurrentSteerAngle : 0f;
                wheel.VisualMesh.localRotation = Quaternion.Euler(0f, steer, 0f) * Quaternion.Euler(wheel.SpinDegrees, 0f, 0f) * Quaternion.Euler(0f, 0f, 90f);
            }
        }
    }
}
