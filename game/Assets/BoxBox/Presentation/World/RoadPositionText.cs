using BoxBox.Roads;
using UnityEngine;

namespace BoxBox.Presentation.World
{
    /// <summary>Debug readout of where the vehicle is in the road graph (lane, limit, wrong way).</summary>
    public static class RoadPositionText
    {
        public static string Describe(RoadNetwork network, Transform vehicle, float speedKmh)
        {
            var p = new RoadPoint(vehicle.position.x, vehicle.position.z);
            RoadSurfaceId id = network.FindSurface(p);
            if (id.Kind == RoadSurfaceKind.None) return "Fora da rua";
            if (id.Kind == RoadSurfaceKind.Junction) return "Cruzamento";

            network.TryGetLanes(id, out RoadLanes lanes);
            int lane = network.GetLane(id, p);
            string text = $"Faixa {lane + 1}/{lanes.Count} · limite {lanes.SpeedLimitKmh:0} km/h";

            float laneHeading = network.LaneHeadingDeg(id, lane, p);
            float delta = Mathf.Abs(Mathf.DeltaAngle(vehicle.eulerAngles.y, laneHeading));
            bool movingForward = speedKmh > 2f, movingBack = speedKmh < -2f;
            if ((movingForward && delta > 100f) || (movingBack && delta < 80f)) text += " · <color=red><b>CONTRAMÃO</b></color>";
            else if (speedKmh > lanes.SpeedLimitKmh + 5f) text += " · <color=orange>acima do limite</color>";
            return text;
        }
    }
}
