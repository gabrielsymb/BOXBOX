using System;
using System.Collections.Generic;
using BoxBox.Roads;
using UnityEngine;

namespace BoxBox.Presentation.World
{
    /// <summary>
    /// GPS line on the ground: from the vehicle, through the junctions the GTA2-style RouteFinder
    /// picks, to the destination. The route starts at the junction ahead, so it never asks for a U-turn
    /// in the middle of a block.
    /// </summary>
    public sealed class RouteGuide : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f, LineHeight = 0.25f;

        private RoadNetwork _network;
        private RouteFinder _finder;
        private Func<Vector3?> _destination;
        private LineRenderer _line;
        private float _nextRefresh;
        private RoadSurfaceId _start, _goal;
        private Vector3 _lastDestination;
        private readonly List<Vector3> _points = new List<Vector3>();

        public float RouteLengthM { get; private set; }

        public void Init(RoadNetwork network, Func<Vector3?> destination)
        {
            _network = network;
            _finder = new RouteFinder(network);
            _destination = destination;

            var go = new GameObject("RouteGuide");
            _line = go.AddComponent<LineRenderer>();
            _line.widthMultiplier = 0.6f;
            _line.numCornerVertices = 4;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.material = LineMaterial();
            // Same look as the first version; only the first metres fade so it doesn't cover the car.
            var colour = new Color(0.2f, 0.85f, 1f);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(colour, 0f), new GradientColorKey(colour, 1f) },
                new[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(0.85f, 0.05f), new GradientAlphaKey(0.85f, 1f) });
            _line.colorGradient = gradient;
            _line.enabled = false;
        }

        // The route is searched again only when the junction ahead or the goal changes; the line's
        // first point follows the car every frame, so it slides instead of jumping.
        private void Update()
        {
            if (_network == null) return;

            Vector3? destination = _destination();
            if (!destination.HasValue) { _line.enabled = false; RouteLengthM = 0f; _start = _goal = RoadSurfaceId.None; _nextRefresh = 0f; return; }

            if (Time.time >= _nextRefresh)
            {
                _nextRefresh = Time.time + RefreshSeconds;
                RoadSurfaceId start = JunctionAhead(transform.position);
                RoadSurfaceId goal = _finder.NearestJunction(new RoadPoint(destination.Value.x, destination.Value.z));
                if (!start.Equals(_start) || !goal.Equals(_goal) || destination.Value != _lastDestination)
                {
                    _start = start; _goal = goal; _lastDestination = destination.Value;
                    RebuildPath(_finder.FindRoute(start, goal), destination.Value);
                }
            }

            if (_points.Count == 0) return;
            _points[0] = new Vector3(transform.position.x, LineHeight, transform.position.z);
            RouteLengthM = 0f;
            for (int i = 1; i < _points.Count; i++) RouteLengthM += Vector3.Distance(_points[i - 1], _points[i]);
            _line.positionCount = _points.Count;
            _line.SetPositions(_points.ToArray());
            _line.enabled = _points.Count > 1;
        }

        private void RebuildPath(List<RoadSurfaceId> route, Vector3 destination)
        {
            _points.Clear();
            _points.Add(Vector3.zero);   // car, set every frame
            foreach (RoadSurfaceId id in route)
            {
                RoadPoint c = _network.Junction(id).Centre;
                _points.Add(new Vector3(c.X, LineHeight, c.Z));
            }
            _points.Add(new Vector3(destination.x, LineHeight, destination.z));
        }

        private RoadSurfaceId JunctionAhead(Vector3 car)
        {
            var p = new RoadPoint(car.x, car.z);
            RoadSurfaceId surface = _network.FindSurface(p);
            if (surface.Kind == RoadSurfaceKind.Junction) return surface;

            RoadStraight street = _network.Straight(surface);
            if (street == null) return _finder.NearestJunction(p);
            RoadPoint dir = street.Direction;
            bool forward = transform.forward.x * dir.X + transform.forward.z * dir.Z >= 0f;
            return street.Connect[forward ? 0 : 1];
        }

        private static Material LineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) return new Material(shader);
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);   // pipeline's default lit material
            var material = new Material(probe.GetComponent<Renderer>().sharedMaterial);
            Destroy(probe);
            return material;
        }

        private void OnDestroy()
        {
            if (_line != null) Destroy(_line.gameObject);
        }
    }
}
