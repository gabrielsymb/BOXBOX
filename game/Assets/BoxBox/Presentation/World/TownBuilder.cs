using System.Collections.Generic;
using BoxBox.Roads;
using UnityEngine;

namespace BoxBox.Presentation.World
{
    /// <summary>
    /// Draws a GridTown as greybox: asphalt per straight and junction, painted lane lines, flat
    /// sidewalks and seeded box buildings. Road paint has no collider (wheels drive on the ground);
    /// buildings are solid. All geometry comes from the road graph, so traffic AI can use the same data.
    /// </summary>
    public static class TownBuilder
    {
        private const float AsphaltY = 0.006f, PaintY = 0.016f, SidewalkY = 0.005f;
        private static readonly Color Asphalt = new Color(0.2f, 0.2f, 0.22f);
        private static readonly Color Sidewalk = new Color(0.62f, 0.6f, 0.56f);
        private static readonly Color Yellow = new Color(0.95f, 0.78f, 0.15f);
        private static readonly Color White = new Color(0.92f, 0.92f, 0.9f);

        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        /// <param name="isBuildable">False for lots that must stay empty (shops, test yard).</param>
        public static Transform Build(GridTown town, System.Func<int, int, bool> isBuildable, int seed)
        {
            var root = new GameObject("Town").transform;
            RoadNetwork net = town.Network;

            foreach (RoadJunction j in net.Junctions)
                Paint(root, "Junction", j.Centre, 0f, j.HalfSizeX * 2f, j.HalfSizeZ * 2f, AsphaltY, 0.01f, Asphalt);

            foreach (RoadStraight s in net.Straights)
            {
                Paint(root, "Street", s.Mid, s.HeadingDeg, s.Lanes.WidthM, s.LengthM, AsphaltY, 0.01f, Asphalt);
                PaintLaneLines(root, s);
            }

            var random = new System.Random(seed);
            for (int c = 0; c < town.Spec.Columns; c++)
                for (int r = 0; r < town.Spec.Rows; r++)
                {
                    GroundRect block = town.Block(c, r);
                    Paint(root, $"Sidewalk_{c}_{r}", block.Centre, 0f, block.SizeX, block.SizeZ, SidewalkY, 0.01f, Sidewalk);
                    if (isBuildable(c, r)) AddBuildings(root, town.Lot(c, r), random);
                }
            return root;
        }

        // Line between lanes k and k+1: yellow where traffic directions meet, white otherwise.
        private static void PaintLaneLines(Transform root, RoadStraight s)
        {
            RoadLanes lanes = s.Lanes;
            for (int k = 0; k < lanes.Count - 1; k++)
            {
                float across = (lanes.HalfCount - k - 1) * RoadLanes.LaneWidthM;
                bool opposite = lanes.IsReversed(k) != lanes.IsReversed(k + 1);
                RoadPoint right = s.Right;
                var centre = new RoadPoint(s.Mid.X + right.X * across, s.Mid.Z + right.Z * across);
                Paint(root, "LaneLine", centre, s.HeadingDeg, opposite ? 0.25f : 0.15f, s.LengthM, PaintY, 0.008f,
                    opposite ? Yellow : White);
            }
        }

        // 2×2 boxes per lot with alleys between them, heights seeded so the town is the same every run.
        private static void AddBuildings(Transform root, GroundRect lot, System.Random random)
        {
            const float alley = 3f;
            float w = (lot.SizeX - alley) / 2f, d = (lot.SizeZ - alley) / 2f;
            for (int i = 0; i < 2; i++)
                for (int k = 0; k < 2; k++)
                {
                    float height = 6f + (float)random.NextDouble() * 18f;
                    float grey = 0.45f + (float)random.NextDouble() * 0.3f;
                    var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    b.name = "Building";
                    b.transform.SetParent(root, false);
                    b.transform.position = new Vector3(lot.MinX + w / 2f + i * (w + alley), height / 2f,
                        lot.MinZ + d / 2f + k * (d + alley));
                    b.transform.localScale = new Vector3(w, height, d);
                    b.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(grey, grey * 0.97f, grey * 0.92f));
                }
        }

        private static void Paint(Transform root, string name, RoadPoint centre, float headingDeg, float width,
            float length, float y, float thickness, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(centre.X, y, centre.Z);
            go.transform.rotation = Quaternion.Euler(0f, headingDeg, 0f);
            go.transform.localScale = new Vector3(width, thickness, length);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
        }

        private static Material MaterialFor(Color color)
        {
            if (Materials.TryGetValue(color, out Material m) && m != null) return m;
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m = new Material(probe.GetComponent<Renderer>().sharedMaterial) { color = color };
            Object.Destroy(probe);
            Materials[color] = m;
            return m;
        }
    }
}
