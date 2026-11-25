using System.Collections.Generic;
using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    /// <summary>
    /// Open cargo bed of a pickup. Crates are real rigidbodies resting on the bed floor:
    /// their weight presses the suspension, they must be pushed along by the truck (so a loaded
    /// truck accelerates and brakes worse), and they slide or fly out in hard turns and jumps.
    /// Nothing here fakes the load; this component only spawns crates and counts them.
    /// </summary>
    public sealed class CargoBed : MonoBehaviour
    {
        [SerializeField] private float crateSize = 0.45f;
        [SerializeField] private float crateMassKg = 25f;
        [SerializeField] private int layers = 2;
        [Tooltip("Friction between crate and bed. Lower = cargo slides out more easily.")]
        [SerializeField] private float crateFriction = 0.55f;

        private readonly List<Rigidbody> _crates = new List<Rigidbody>();
        private Bounds _localBed;
        private PhysicsMaterial _material;

        public int CrateCount => _crates.Count;
        public bool IsLoaded => _crates.Count > 0;

        public void SetBedVolume(Bounds localBed) => _localBed = localBed;

        public void ToggleLoad()
        {
            if (IsLoaded) Unload();
            else Load();
        }

        public int CountOnBoard()
        {
            int count = 0;
            foreach (Rigidbody crate in _crates)
            {
                Vector3 local = transform.InverseTransformPoint(crate.position);
                Bounds bed = _localBed;
                bed.Expand(new Vector3(0.1f, 1.5f, 0.1f));
                if (bed.Contains(local)) count++;
            }
            return count;
        }

        public float LoadedMassKg() => CountOnBoard() * crateMassKg;

        private void Load()
        {
            float step = crateSize + 0.03f;
            int cols = Mathf.FloorToInt(_localBed.size.x / step);
            int rows = Mathf.FloorToInt(_localBed.size.z / step);
            Vector3 origin = _localBed.min + new Vector3(step / 2f, crateSize / 2f + 0.02f, step / 2f);
            Vector3 velocity = GetComponent<Rigidbody>().linearVelocity;

            for (int y = 0; y < layers; y++)
                for (int z = 0; z < rows; z++)
                    for (int x = 0; x < cols; x++)
                        SpawnCrate(origin + new Vector3(x * step, y * (crateSize + 0.02f), z * step), velocity);
        }

        private void SpawnCrate(Vector3 localPos, Vector3 velocity)
        {
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.position = transform.TransformPoint(localPos);
            crate.transform.rotation = transform.rotation;
            crate.transform.localScale = Vector3.one * crateSize;
            crate.GetComponent<Collider>().sharedMaterial = CrateMaterial();

            var rb = crate.AddComponent<Rigidbody>();
            rb.mass = crateMassKg;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = velocity;
            _crates.Add(rb);
        }

        private void Unload()
        {
            foreach (Rigidbody crate in _crates)
                if (crate != null) Destroy(crate.gameObject);
            _crates.Clear();
        }

        private PhysicsMaterial CrateMaterial()
        {
            if (_material != null) return _material;
            _material = new PhysicsMaterial("Crate")
            {
                dynamicFriction = crateFriction,
                staticFriction = crateFriction * 1.15f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0.05f
            };
            return _material;
        }
    }
}
