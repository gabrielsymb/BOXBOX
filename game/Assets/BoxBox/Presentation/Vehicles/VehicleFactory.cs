using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    /// <summary>
    /// Builds a drivable vehicle (body, rigidbody, wheels, physics) from a VehicleSpec. Used for the
    /// player and for civilian traffic, which share the same physics like in Driver 2.
    /// </summary>
    public static class VehicleFactory
    {
        public static VehiclePhysicsController Create(VehicleSpec spec, Vector3 position, float headingDeg,
            string name, out CargoBed cargo)
        {
            var vehicleObj = new GameObject(name);
            vehicleObj.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, headingDeg, 0f));

            var rb = vehicleObj.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Origin = ground level at rest (Driver places wheel contact points there).
            cargo = null;
            Bounds? bed = VehicleBodyBuilder.Build(vehicleObj.transform, spec);
            if (bed.HasValue)
            {
                cargo = vehicleObj.AddComponent<CargoBed>();
                cargo.SetBedVolume(bed.Value);
            }

            var controller = vehicleObj.AddComponent<VehiclePhysicsController>();
            controller.SetSpec(spec);
            AddWheels(controller, vehicleObj.transform, spec);
            return controller;
        }

        /// <summary>Paints every body part (not the wheels).</summary>
        public static void Paint(VehiclePhysicsController vehicle, Color color)
        {
            foreach (Renderer r in vehicle.GetComponentsInChildren<Renderer>())
                if (!r.name.StartsWith("WheelMesh")) r.material.color = color;
        }

        private static void AddWheels(VehiclePhysicsController controller, Transform root, VehicleSpec spec)
        {
            float x = spec.track / 2f, z = spec.wheelbase / 2f;
            // Tyre contact points (Driver wheelDisp): the -90 degree point of each wheel at rest.
            Vector3[] wheelOffsets =
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
                Object.Destroy(visual.GetComponent<Collider>());

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
    }
}
