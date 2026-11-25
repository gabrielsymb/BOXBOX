using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    /// <summary>
    /// Builds the playable physics test vehicle and HUD in the SandboxLab scene.
    /// </summary>
    public sealed class SandboxVehicleBuilder : MonoBehaviour
    {
        [Tooltip("Vehicle characteristics. Empty = default Driver-like car.")]
        [SerializeField] private VehicleSpec vehicleSpec;

        private VehiclePhysicsController _vehicle;
        private VehicleChaseCamera _camera;

        private void Start()
        {
            SetupVehicle();
            SetupCamera();
            SetupTestCourse();
        }

        // Ramp + slalom pillars so suspension, weight transfer and slides can be felt.
        private void SetupTestCourse()
        {
            var course = new GameObject("TestCourse").transform;

            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.SetParent(course, false);
            ramp.transform.position = new Vector3(0f, 0.6f, 45f);
            ramp.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            ramp.transform.localScale = new Vector3(6f, 0.5f, 8f);

            for (int i = 0; i < 6; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Slalom_" + i;
                pillar.transform.SetParent(course, false);
                pillar.transform.position = new Vector3(-20f, 0.75f, 10f + i * 14f);
                pillar.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
            }
        }

        private void SetupVehicle()
        {
            if (vehicleSpec == null) vehicleSpec = ScriptableObject.CreateInstance<VehicleSpec>();
            VehicleSpec spec = vehicleSpec;
            float bodyBottom = spec.groundClearance;
            float bodyMid = bodyBottom + spec.bodySize.y / 2f;

            var vehicleObj = new GameObject("PickupTruck_Physics");
            vehicleObj.transform.position = new Vector3(0f, 0.5f, 0f);

            var rb = vehicleObj.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Chassis Collider. Origin = ground level at rest (Driver places wheel contact points there);
            // 0.4 m clearance keeps the body off the ground until the springs bottom out.
            var collider = vehicleObj.AddComponent<BoxCollider>();
            collider.size = spec.bodySize;
            collider.center = new Vector3(0f, bodyMid, 0f);

            // Visual Body Placeholder
            var bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyObj.name = "Chassis_Cabin";
            bodyObj.transform.SetParent(vehicleObj.transform, false);
            bodyObj.transform.localPosition = new Vector3(0f, bodyMid + 0.1f, spec.bodySize.z * 0.14f);
            bodyObj.transform.localScale = new Vector3(spec.bodySize.x * 0.97f, spec.bodySize.y * 1.2f, spec.bodySize.z * 0.5f);
            Destroy(bodyObj.GetComponent<Collider>());

            var bedObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bedObj.name = "Chassis_Bed";
            bedObj.transform.SetParent(vehicleObj.transform, false);
            bedObj.transform.localPosition = new Vector3(0f, bodyBottom + 0.3f, -spec.bodySize.z * 0.3f);
            bedObj.transform.localScale = new Vector3(spec.bodySize.x * 0.97f, 0.6f, spec.bodySize.z * 0.4f);
            Destroy(bedObj.GetComponent<Collider>());

            // Wheels setup
            var controller = vehicleObj.AddComponent<VehiclePhysicsController>();
            controller.SetSpec(spec);
            SetupWheels(controller, vehicleObj.transform, spec);

            vehicleObj.AddComponent<VehicleInputAdapter>();
            _vehicle = controller;
        }

        private void SetupWheels(VehiclePhysicsController controller, Transform root, VehicleSpec spec)
        {
            float x = spec.track / 2f, z = spec.wheelbase / 2f;
            // Tyre contact points (Driver wheelDisp): the -90 degree point of each wheel at rest.
            Vector3[] wheelOffsets = new[]
            {
                new Vector3(-x, 0f, z),  // Front-Left
                new Vector3(x, 0f, z),   // Front-Right
                new Vector3(-x, 0f, -z), // Rear-Left
                new Vector3(x, 0f, -z)   // Rear-Right
            };

            var wheels = new VehiclePhysicsController.Wheel[4];
            string[] names = { "FL", "FR", "RL", "RR" };

            for (int i = 0; i < 4; i++)
            {
                var mount = new GameObject("Mount_" + names[i]);
                mount.transform.SetParent(root, false);
                mount.transform.localPosition = wheelOffsets[i];

                var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "WheelMesh_" + names[i];
                visual.transform.SetParent(mount.transform, false);
                float d = spec.wheelRadius * 2f;   // Unity cylinder: radius 0.5, height 2
                visual.transform.localScale = new Vector3(d, 0.15f, d);   // 0.3 m wide tyre
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Destroy(visual.GetComponent<Collider>());

                wheels[i] = new VehiclePhysicsController.Wheel
                {
                    Name = names[i],
                    MountPoint = mount.transform,
                    VisualMesh = visual.transform,
                    IsFront = i < 2
                };
            }

            controller.SetWheels(wheels);
        }

        private void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                _camera = mainCam.gameObject.AddComponent<VehicleChaseCamera>();
                _camera.SetTarget(_vehicle.transform);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) ResetVehicle();
        }

        private void OnGUI()
        {
            if (_vehicle == null) return;

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(16f, 16f, 340f, 260f), GUI.skin.box);
            GUILayout.Label("<b>BOXBOX | Física do Veículo (REDRIVER2)</b>");
            GUILayout.Label($"Velocidade: <b>{_vehicle.SpeedKmh:0} km/h</b>");
            GUILayout.Label($"Esterçamento: <b>{_vehicle.CurrentSteerAngle:0.0}°</b>");
            GUILayout.Label($"Teclado detectado: <b>{(Input.anyKey ? "SIM" : "NÃO")}</b>");
            GUILayout.Space(6f);

            GUILayout.Label("<b>Controles na Tela (ou teclado W/A/S/D):</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("▲ Acelerar (W)")) _vehicle.SetInputs(1f, 0f, false);
            if (GUILayout.RepeatButton("▼ Frear/Ré (S)")) _vehicle.SetInputs(-1f, 0f, false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("◀ Virar Esq (A)")) _vehicle.SetInputs(0.5f, -1f, false);
            if (GUILayout.RepeatButton("▶ Virar Dir (D)")) _vehicle.SetInputs(0.5f, 1f, false);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Câmera (C): " + (_camera != null ? _camera.CurrentMode.ToString() : "")))
            {
                _camera?.ToggleMode();
            }
            if (GUILayout.Button("Reset (R)"))
            {
                ResetVehicle();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void ResetVehicle()
        {
            if (_vehicle == null) return;
            Transform tr = _vehicle.transform;
            tr.position = new Vector3(tr.position.x, 0.5f, tr.position.z);
            tr.rotation = Quaternion.Euler(0f, tr.eulerAngles.y, 0f);
            Rigidbody rb = _vehicle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
