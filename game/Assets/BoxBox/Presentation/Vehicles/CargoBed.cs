using System.Collections.Generic;
using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    /// <summary>
    /// Open cargo bed of a pickup. Each crate is a real rigidbody tagged with the product it
    /// holds: its weight presses the suspension, the truck must push it along (a loaded truck
    /// accelerates and brakes worse), and it slides or flies out in hard turns and jumps.
    /// Nothing here fakes the load; this component only spawns crates and counts them.
    /// </summary>
    public sealed class CargoBed : MonoBehaviour
    {
        [SerializeField] private float crateSize = 0.45f;
        [SerializeField] private float crateMassKg = 25f;
        [SerializeField] private int layers = 2;
        [Tooltip("Friction between crate and bed. Lower = cargo slides out more easily.")]
        [SerializeField] private float crateFriction = 0.55f;

        private struct Crate
        {
            public Rigidbody Body;
            public string ProductId;
        }

        private readonly List<Crate> _crates = new List<Crate>();
        private Bounds _localBed;
        private PhysicsMaterial _material;

        public int CrateCount => _crates.Count;
        public int Capacity => Columns * Rows * layers;
        private float Step => crateSize + 0.03f;
        private int Columns => Mathf.FloorToInt(_localBed.size.x / Step);
        private int Rows => Mathf.FloorToInt(_localBed.size.z / Step);

        public void SetBedVolume(Bounds localBed) => _localBed = localBed;

        public bool HasFreeSlot()
        {
            for (int slot = 0; slot < Capacity; slot++)
                if (!IsSlotTaken(SlotPosition(slot))) return true;
            return false;
        }

        /// <summary>Puts one crate of the product in the first free slot. False if the bed is full.</summary>
        public bool TryAddCrate(string productId, Color color)
        {
            for (int slot = 0; slot < Capacity; slot++)
            {
                Vector3 local = SlotPosition(slot);
                if (IsSlotTaken(local)) continue;
                SpawnCrate(local, productId, color);
                return true;
            }
            return false;
        }

        public int CountOnBoard() => CountOnBoard(null);

        /// <summary>Crates of this product still inside the bed (null = any product).</summary>
        public int CountOnBoard(string productId)
        {
            int count = 0;
            foreach (Crate crate in _crates)
                if (crate.Body != null && (productId == null || crate.ProductId == productId) && IsOnBoard(crate.Body))
                    count++;
            return count;
        }

        public float LoadedMassKg() => CountOnBoard() * crateMassKg;

        /// <summary>Removes every crate, on board or lying on the road.</summary>
        public void ClearAll()
        {
            foreach (Crate crate in _crates)
                if (crate.Body != null) Destroy(crate.Body.gameObject);
            _crates.Clear();
        }

        private bool IsOnBoard(Rigidbody body)
        {
            Bounds bed = _localBed;
            bed.Expand(new Vector3(0.1f, 1.5f, 0.1f));
            return bed.Contains(transform.InverseTransformPoint(body.position));
        }

        private Vector3 SlotPosition(int slot)
        {
            int perLayer = Columns * Rows;
            int y = slot / perLayer, z = slot % perLayer / Columns, x = slot % Columns;
            Vector3 origin = _localBed.min + new Vector3(Step / 2f, crateSize / 2f + 0.02f, Step / 2f);
            return origin + new Vector3(x * Step, y * (crateSize + 0.02f), z * Step);
        }

        // A crate that slid around may be sitting in a slot; don't spawn into it.
        private bool IsSlotTaken(Vector3 localSlot)
        {
            foreach (Crate crate in _crates)
            {
                if (crate.Body == null) continue;
                Vector3 local = transform.InverseTransformPoint(crate.Body.position);
                if ((local - localSlot).sqrMagnitude < crateSize * crateSize * 0.64f) return true;
            }
            return false;
        }

        private void SpawnCrate(Vector3 localPos, string productId, Color color)
        {
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate_" + productId;
            crate.transform.position = transform.TransformPoint(localPos);
            crate.transform.rotation = transform.rotation;
            crate.transform.localScale = Vector3.one * crateSize;
            crate.GetComponent<Collider>().sharedMaterial = CrateMaterial();
            crate.GetComponent<Renderer>().material.color = color;

            var rb = crate.AddComponent<Rigidbody>();
            rb.mass = crateMassKg;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = GetComponent<Rigidbody>().linearVelocity;
            _crates.Add(new Crate { Body = rb, ProductId = productId });
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
