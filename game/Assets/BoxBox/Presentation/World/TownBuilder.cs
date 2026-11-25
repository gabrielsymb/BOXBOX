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
        private const float AsphaltY = 0.006f, PaintY = 0.016f;
        /// <summary>Kerb height. The wheels climb it (suspension ray starts 0.6 m up), cars feel the bump.</summary>
        public const float KerbHeightM = 0.15f;
        private static readonly Color Asphalt = new Color(0.2f, 0.2f, 0.22f);
        private static readonly Color Sidewalk = new Color(0.62f, 0.6f, 0.56f);
        private static readonly Color Yellow = new Color(0.95f, 0.78f, 0.15f);
        private static readonly Color White = new Color(0.92f, 0.92f, 0.9f);

        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        /// <summary>Opening in a block's kerb (driveway) on one side, centred at a coordinate along it.</summary>
        public readonly struct Driveway
        {
            public readonly int Column, Row;
            public readonly RoadExit Side;
            public readonly float Centre, Width;
            public Driveway(int column, int row, RoadExit side, float centre, float width)
            {
                Column = column; Row = row; Side = side; Centre = centre; Width = width;
            }
        }

        /// <param name="isBuildable">False for lots that must stay empty (shops, test yard).</param>
        public static Transform Build(GridTown town, System.Func<int, int, bool> isBuildable, int seed,
            IReadOnlyList<Driveway> driveways = null)
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
                    AddSidewalks(root, town.Block(c, r), town.Spec.SidewalkM, c, r, driveways);
                    if (isBuildable(c, r)) AddBuildings(root, town.Lot(c, r), random);
                }
            return root;
        }

        // Four raised strips around the lot; each driveway cuts a gap (a lowered kerb) in its side.
        private static void AddSidewalks(Transform root, GroundRect block, float width, int c, int r,
            IReadOnlyList<Driveway> driveways)
        {
            for (int side = 0; side < 4; side++)
            {
                var exit = (RoadExit)side;
                bool northSouth = exit == RoadExit.North || exit == RoadExit.South;
                // N/S strips run along X over the full block; E/W strips fill between them.
                float from = northSouth ? block.MinX : block.MinZ + width;
                float to = northSouth ? block.MaxX : block.MaxZ - width;
                float fixedCoord = exit == RoadExit.North ? block.MaxZ - width / 2f : exit == RoadExit.South ? block.MinZ + width / 2f
                                 : exit == RoadExit.East ? block.MaxX - width / 2f : block.MinX + width / 2f;

                var gaps = new List<(float, float)>();
                if (driveways != null)
                    foreach (Driveway d in driveways)
                        if (d.Column == c && d.Row == r && d.Side == exit) gaps.Add((d.Centre - d.Width / 2f, d.Centre + d.Width / 2f));
                gaps.Sort((a, b) => a.Item1.CompareTo(b.Item1));

                float start = from;
                foreach ((float g0, float g1) in gaps)
                {
                    Kerb(root, northSouth, fixedCoord, start, Mathf.Min(g0, to), width);
                    start = Mathf.Max(start, g1);
                }
                Kerb(root, northSouth, fixedCoord, start, to, width);
            }
        }

        private static void Kerb(Transform root, bool alongX, float fixedCoord, float from, float to, float width)
        {
            if (to - from < 0.05f) return;
            float mid = (from + to) / 2f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Sidewalk";
            go.transform.SetParent(root, false);
            go.transform.position = alongX ? new Vector3(mid, KerbHeightM / 2f, fixedCoord) : new Vector3(fixedCoord, KerbHeightM / 2f, mid);
            go.transform.localScale = alongX ? new Vector3(to - from, KerbHeightM, width) : new Vector3(width, KerbHeightM, to - from);
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(Sidewalk);
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
