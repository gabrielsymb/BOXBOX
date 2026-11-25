using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    /// <summary>
    /// Builds a greybox vehicle body from a VehicleSpec. Each part is a primitive whose collider
    /// is kept, so the Rigidbody gets a compound collider that matches what you see.
    /// With spec.bedLength > 0 the body is a pickup: chassis slab, cab, and an open bed with walls
    /// that cargo can physically sit in, slide around and fall out of.
    /// </summary>
    public static class VehicleBodyBuilder
    {
        private const float WallThickness = 0.08f;

        /// <summary>Builds the body under root. Returns the bed's local volume, or null if there is no bed.</summary>
        public static Bounds? Build(Transform root, VehicleSpec spec)
        {
            return spec.bedLength > 0f ? BuildPickup(root, spec) : BuildClosedBody(root, spec);
        }

        private static Bounds? BuildClosedBody(Transform root, VehicleSpec spec)
        {
            Vector3 s = spec.bodySize;
            float mid = spec.groundClearance + s.y / 2f;
            Part(root, "Body", new Vector3(0f, mid, 0f), s);
            return null;
        }

        private static Bounds? BuildPickup(Transform root, VehicleSpec spec)
        {
            Vector3 s = spec.bodySize;
            float floorY = spec.groundClearance + spec.chassisHeight;
            float cabLength = s.z - spec.bedLength;
            float cabHeight = s.y - spec.chassisHeight;
            float bedCentreZ = -s.z / 2f + spec.bedLength / 2f;
            float wallY = floorY + spec.bedWallHeight / 2f;
            float sideX = s.x / 2f - WallThickness / 2f;

            Part(root, "Chassis", new Vector3(0f, spec.groundClearance + spec.chassisHeight / 2f, 0f),
                new Vector3(s.x, spec.chassisHeight, s.z));
            Part(root, "Cab", new Vector3(0f, floorY + cabHeight / 2f, s.z / 2f - cabLength / 2f),
                new Vector3(s.x * 0.97f, cabHeight, cabLength));
            Part(root, "Bed_Left", new Vector3(-sideX, wallY, bedCentreZ),
                new Vector3(WallThickness, spec.bedWallHeight, spec.bedLength));
            Part(root, "Bed_Right", new Vector3(sideX, wallY, bedCentreZ),
                new Vector3(WallThickness, spec.bedWallHeight, spec.bedLength));
            Part(root, "Bed_Tailgate", new Vector3(0f, wallY, -s.z / 2f + WallThickness / 2f),
                new Vector3(s.x, spec.bedWallHeight, WallThickness));

            var inner = new Vector3(s.x - 2f * WallThickness, spec.bedWallHeight, spec.bedLength - WallThickness);
            return new Bounds(new Vector3(0f, wallY, bedCentreZ + WallThickness / 2f), inner);
        }

        private static void Part(Transform root, string name, Vector3 localPos, Vector3 size)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(root, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = size;
        }
    }
}
