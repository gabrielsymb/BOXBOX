using BoxBox.Presentation.Supply;
using BoxBox.Presentation.Traffic;
using BoxBox.Presentation.World;
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
        private CargoBed _cargo;
        private SandboxTown _town;

        private void Start()
        {
            _town = new SandboxTown();
            _town.Build();
            SetupVehicle();
            SetupCamera();
            SetupTestCourse(_town.TestYardCentre);
            SetupSupplyRun();
            gameObject.AddComponent<TrafficManager>().Init(_town.Town.Network, _vehicle.transform, count: 14, seed: 2026);
        }

        // Grocery at the spawn point, wholesaler across the avenues: the first playable supply run.
        private void SetupSupplyRun()
        {
            if (_cargo == null) return;
            var supply = _vehicle.gameObject.AddComponent<SupplyRunController>();
            supply.Init(_vehicle, _cargo, _town.StorePad, _town.WholesalerPad);
            _vehicle.gameObject.AddComponent<RouteGuide>().Init(_town.Town.Network, () => supply.Destination);
        }

        // Ramp + slalom pillars in the empty lot across the street, so suspension, weight transfer
        // and slides can be felt.
        private void SetupTestCourse(Vector3 yard)
        {
            var course = new GameObject("TestCourse").transform;

            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp";
            ramp.transform.SetParent(course, false);
            ramp.transform.position = yard + new Vector3(8f, 0.6f, 0f);
            ramp.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            ramp.transform.localScale = new Vector3(6f, 0.5f, 8f);

            for (int i = 0; i < 6; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Slalom_" + i;
                pillar.transform.SetParent(course, false);
                pillar.transform.position = yard + new Vector3(-12f, 0.75f, -20f + i * 8f);
                pillar.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
            }
        }

        private void SetupVehicle()
        {
            if (vehicleSpec == null) vehicleSpec = ScriptableObject.CreateInstance<VehicleSpec>();
            string name = vehicleSpec.bedLength > 0f ? "Pickup_Physics" : "Car_Physics";
            _vehicle = VehicleFactory.Create(vehicleSpec, new Vector3(0f, 0.5f, 0f), 0f, name, out _cargo);
            _vehicle.gameObject.AddComponent<VehicleInputAdapter>();
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
            if (_vehicle != null && _vehicle.transform.position.y < -10f) RescueFromLimbo();
        }

        private void OnGUI()
        {
            if (_vehicle == null) return;

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(16f, 16f, 340f, 300f), GUI.skin.box);
            GUILayout.Label("<b>BOXBOX | Física do Veículo (REDRIVER2)</b>");
            GUILayout.Label($"Velocidade: <b>{_vehicle.SpeedKmh:0} km/h</b>");
            GUILayout.Label($"Esterçamento: <b>{_vehicle.CurrentSteerAngle:0.0}°</b>");
            GUILayout.Label(RoadPositionText.Describe(_town.Town.Network, _vehicle.transform, _vehicle.SpeedKmh));
            if (_cargo != null)
            {
                GUILayout.Label($"Caçamba: <b>{_cargo.CountOnBoard()}/{_cargo.Capacity} caixas</b> ({_cargo.LoadedMassKg():0} kg)");
            }
            GUILayout.Space(6f);

            GUILayout.Label("<b>W acelera · S/Espaço freia · S de novo parado = ré · Ctrl freio de mão</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("▲ Acelerar (W)")) _vehicle.SetInputs(1f, 0f, false);
            if (GUILayout.RepeatButton("■ Freio (Espaço)")) _vehicle.SetInputs(0f, 0f, false, false, true);
            if (GUILayout.RepeatButton("▼ Freio/Ré (S)")) _vehicle.SetInputs(-1f, 0f, false);
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

        // Fell off the world: put the car back on the closest crossing instead of losing it.
        private void RescueFromLimbo()
        {
            Vector3 p = _vehicle.transform.position;
            var finder = new BoxBox.Roads.RouteFinder(_town.Town.Network);
            BoxBox.Roads.RoadSurfaceId j = finder.NearestJunction(new BoxBox.Roads.RoadPoint(p.x, p.z));
            BoxBox.Roads.RoadPoint c = _town.Town.Network.Junction(j).Centre;
            _vehicle.transform.position = new Vector3(c.X, 0.5f, c.Z);
            ResetVehicle();
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
