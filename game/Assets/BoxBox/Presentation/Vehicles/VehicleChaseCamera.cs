using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    /// <summary>
    /// Dual-mode camera adapted from Driver 2 (camera.c PlaceCameraFollowCar) and BOXBOX diorama.
    /// Uses critically-damped spring smoothing (SmoothDamp) to eliminate stutter and jitter.
    /// Toggle mode with 'C' key or OnGUI button.
    /// </summary>
    public sealed class VehicleChaseCamera : MonoBehaviour
    {
        public enum CameraMode
        {
            ChaseDriver2,
            DioramaIsometric
        }

        [Header("Target")]
        [SerializeField] private Transform targetVehicle;

        [Header("Mode")]
        [SerializeField] private CameraMode mode = CameraMode.ChaseDriver2;

        [Header("Driver 2 Chase Camera Settings (camera.c)")]
        [SerializeField] private float chaseDistance = 6.0f;
        [SerializeField] private float chaseHeight = 2.2f;
        [SerializeField] private float followSmoothTime = 0.12f;
        [Tooltip("Yaw follow speed in normal driving: high = camera stays glued behind the car.")]
        [SerializeField] private float rotationSmoothSpeed = 10.0f;
        [Tooltip("Yaw follow speed while drifting: low = camera swings out and shows the slide.")]
        [SerializeField] private float driftRotationSmoothSpeed = 2.0f;
        [Tooltip("Slip angle (car heading vs travel direction) where the drift camera fully kicks in.")]
        [SerializeField] private float driftSlipDegrees = 25f;
        [SerializeField] private float lookAheadDistance = 2.0f;

        [Header("Diorama Isometric Settings")]
        [SerializeField] private Vector3 dioramaOffset = new Vector3(-12f, 15f, -12f);
        [SerializeField] private float dioramaSmoothTime = 0.15f;
        [SerializeField] private Vector3 dioramaEulerAngles = new Vector3(38f, 45f, 0f);

        private Camera _cam;
        private Rigidbody _targetRb;
        private Vector3 _currentVelocity;
        private float _currentCamYaw;

        public CameraMode CurrentMode => mode;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (targetVehicle != null)
                SetTarget(targetVehicle);
        }

        public void SetTarget(Transform target)
        {
            targetVehicle = target;
            _targetRb = target != null ? target.GetComponent<Rigidbody>() : null;
            if (targetVehicle != null)
            {
                _currentCamYaw = targetVehicle.eulerAngles.y;
            }
        }

        public void ToggleMode()
        {
            mode = mode == CameraMode.ChaseDriver2 ? CameraMode.DioramaIsometric : CameraMode.ChaseDriver2;
            _currentVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
                ToggleMode();
        }

        private void LateUpdate()
        {
            if (targetVehicle == null) return;

            if (mode == CameraMode.ChaseDriver2)
            {
                UpdateDriver2Chase();
            }
            else
            {
                UpdateDioramaView();
            }
        }

        private void UpdateDriver2Chase()
        {
            // Smooth yaw tracking (Driver 2 angleDelta smoothing in camera.c line 483)
            float targetYaw = targetVehicle.eulerAngles.y;
            float yawDelta = Mathf.DeltaAngle(_currentCamYaw, targetYaw);
            _currentCamYaw += yawDelta * CurrentYawFollowSpeed() * Time.deltaTime;

            Quaternion camRotation = Quaternion.Euler(12f, _currentCamYaw, 0f);

            // Compute ideal camera position behind the car
            Vector3 behindOffset = camRotation * new Vector3(0f, chaseHeight, -chaseDistance);
            Vector3 desiredPosition = targetVehicle.position + behindOffset;

            // Critically-damped position smoothing (SmoothDamp eliminates jitter completely)
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, followSmoothTime);

            // Look slightly ahead of the car center
            Vector3 lookTarget = targetVehicle.position + (targetVehicle.forward * lookAheadDistance) + (Vector3.up * 0.8f);
            Vector3 lookDirection = lookTarget - transform.position;
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        // Cinematic swing only when the car is actually sliding, so ordinary curves stay tight.
        private float CurrentYawFollowSpeed()
        {
            if (_targetRb == null) return rotationSmoothSpeed;
            Vector3 v = _targetRb.linearVelocity; v.y = 0f;
            if (v.magnitude < 5f) return rotationSmoothSpeed;
            float slip = Vector3.Angle(Vector3.ProjectOnPlane(targetVehicle.forward, Vector3.up), v);
            if (slip > 90f) slip = 180f - slip;   // reversing
            float drift01 = Mathf.InverseLerp(8f, driftSlipDegrees, slip);
            return Mathf.Lerp(rotationSmoothSpeed, driftRotationSmoothSpeed, drift01);
        }

        private void UpdateDioramaView()
        {
            // Fixed isometric orientation (does NOT rotate with vehicle)
            transform.rotation = Quaternion.Euler(dioramaEulerAngles);

            // Smooth position following above the vehicle
            Vector3 desiredPosition = targetVehicle.position + dioramaOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, dioramaSmoothTime);
        }
    }
}
