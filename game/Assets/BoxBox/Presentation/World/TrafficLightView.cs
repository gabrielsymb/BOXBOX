using System.Collections.Generic;
using BoxBox.Roads;
using BoxBox.Roads.Traffic;
using UnityEngine;

namespace BoxBox.Presentation.World
{
    /// <summary>
    /// Draws a traffic light for every approach of every crossing (3+ exits), on the driver's right
    /// before the junction, and colours it from TrafficLights each frame. All north/south heads share
    /// one state and all east/west heads another, like Driver's two junctionLightsPhase entries.
    /// </summary>
    public sealed class TrafficLightView : MonoBehaviour
    {
        private readonly List<Renderer>[] _lamps = { new List<Renderer>(), new List<Renderer>() };   // [0] N/S, [1] E/W
        private readonly LightPhase[] _shown = { 0, 0 };
        private Material _red, _amber, _green;

        public void Build(RoadNetwork network)
        {
            _red = Lamp(new Color(1f, 0.1f, 0.05f));
            _amber = Lamp(new Color(1f, 0.7f, 0.05f));
            _green = Lamp(new Color(0.1f, 1f, 0.3f));
            Material pole = Lamp(new Color(0.15f, 0.15f, 0.16f));

            foreach (RoadJunction j in network.Junctions)
            {
                int exits = 0;
                foreach (RoadSurfaceId e in j.Exits) if (e.Kind == RoadSurfaceKind.Straight) exits++;
                if (exits < 3) continue;

                for (int e = 0; e < 4; e++)
                {
                    RoadStraight street = network.Straight(j.Exits[e]);
                    if (street == null) continue;
                    float exitHeading = e * 90f;                          // N, E, S, W
                    RoadPoint outward = RoadPoint.FromHeading(exitHeading);
                    RoadPoint right = RoadPoint.FromHeading(exitHeading + 180f + 90f);   // driver's right
                    float half = (e % 2 == 0) ? j.HalfSizeZ : j.HalfSizeX;
                    float side = street.Lanes.WidthM / 2f + 1.2f;
                    var basePos = new Vector3(j.Centre.X + outward.X * (half + 1f) + right.X * side, 0f,
                                              j.Centre.Z + outward.Z * (half + 1f) + right.Z * side);
                    Head(basePos, exitHeading, pole, e % 2);
                }
            }
        }

        private void Head(Vector3 basePos, float facingDeg, Material poleMaterial, int axis)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "TrafficLight";
            post.transform.SetParent(transform, false);
            post.transform.position = basePos + Vector3.up * 1.6f;
            post.transform.localScale = new Vector3(0.15f, 1.6f, 0.15f);
            post.GetComponent<Renderer>().sharedMaterial = poleMaterial;

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Head";
            box.transform.SetParent(post.transform.parent, false);
            box.transform.position = basePos + Vector3.up * 3.5f;
            box.transform.rotation = Quaternion.Euler(0f, facingDeg, 0f);
            box.transform.localScale = new Vector3(0.4f, 0.9f, 0.3f);
            box.GetComponent<Renderer>().sharedMaterial = poleMaterial;
            Destroy(box.GetComponent<Collider>());

            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Lamp";
            lamp.transform.SetParent(box.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 0f, 0.55f);   // on the face towards the drivers
            lamp.transform.localScale = new Vector3(0.7f, 0.35f, 0.3f);
            Destroy(lamp.GetComponent<Collider>());
            _lamps[axis].Add(lamp.GetComponent<Renderer>());
        }

        private void Update()
        {
            double t = Time.timeAsDouble;
            Show(0, TrafficLights.PhaseAt(RoadExit.North, t));
            Show(1, TrafficLights.PhaseAt(RoadExit.East, t));
        }

        private void Show(int axis, LightPhase phase)
        {
            if (_shown[axis] == phase) return;
            _shown[axis] = phase;
            Material m = phase == LightPhase.Green ? _green : phase == LightPhase.Amber ? _amber : _red;
            foreach (Renderer r in _lamps[axis]) r.sharedMaterial = m;
        }

        private static Material Lamp(Color color)
        {
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var m = new Material(probe.GetComponent<Renderer>().sharedMaterial) { color = color };
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 1.5f);
            }
            Destroy(probe);
            return m;
        }
    }
}
