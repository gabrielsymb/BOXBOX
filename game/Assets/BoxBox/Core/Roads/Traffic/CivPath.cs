using System;
using System.Collections.Generic;

namespace BoxBox.Roads.Traffic
{
    /// <summary>One point of a civilian car's route (Driver's CIV_ROUTE_ENTRY).</summary>
    public readonly struct CivNode
    {
        public RoadPoint Pos { get; }
        public float SpeedLimitKmh { get; }
        /// <summary>Lane end at a junction: the car must obey the light here.</summary>
        public bool IsStopLine { get; }
        public RoadExit Approach { get; }

        public CivNode(RoadPoint pos, float speedLimitKmh, bool isStopLine = false, RoadExit approach = RoadExit.North)
        {
            Pos = pos; SpeedLimitKmh = speedLimitKmh; IsStopLine = isStopLine; Approach = approach;
        }
    }

    /// <summary>
    /// The nodes ahead of a car. Ported from civ_ai.c CivFindStation (project the car on the current
    /// segment, drop segments it has passed) and CivFindPointOnPath (walk a distance along the nodes).
    /// Driver keeps 13 entries in a ring buffer and refills with CreateNewNode; here a list is refilled
    /// by the route builder.
    /// </summary>
    public sealed class CivPath
    {
        private readonly List<CivNode> _nodes = new List<CivNode>();

        public IReadOnlyList<CivNode> Nodes => _nodes;
        public int Count => _nodes.Count;
        public void Add(CivNode node) => _nodes.Add(node);

        /// <summary>Distance of the car along the first segment; earlier segments are discarded.</summary>
        public float FindStation(RoadPoint car)
        {
            while (_nodes.Count >= 2)
            {
                RoadPoint s = _nodes[0].Pos, e = _nodes[1].Pos;
                float dx = e.X - s.X, dz = e.Z - s.Z;
                float length = (float)Math.Sqrt(dx * dx + dz * dz);
                if (length > 0f)
                {
                    float station = ((car.X - s.X) * dx + (car.Z - s.Z) * dz) / length;
                    if (station < length) return station;
                }
                _nodes.RemoveAt(0);
            }
            return 0f;
        }

        /// <summary>Point a distance along the path from the first node; clamps to the last node.</summary>
        public RoadPoint PointOnPath(float station)
        {
            if (_nodes.Count == 0) return default;
            if (station < 0f) station = 0f;
            for (int i = 0; i + 1 < _nodes.Count; i++)
            {
                RoadPoint s = _nodes[i].Pos, e = _nodes[i + 1].Pos;
                float dx = e.X - s.X, dz = e.Z - s.Z;
                float length = (float)Math.Sqrt(dx * dx + dz * dz);
                if (station < length)
                    return new RoadPoint(s.X + dx * station / length, s.Z + dz * station / length);
                station -= length;
            }
            return _nodes[_nodes.Count - 1].Pos;
        }

        /// <summary>Length still ahead from a station on the first segment to the end of the path.</summary>
        public float RemainingLength(float station)
        {
            float total = -station;
            for (int i = 0; i + 1 < _nodes.Count; i++) total += Distance(_nodes[i].Pos, _nodes[i + 1].Pos);
            return total;
        }

        /// <summary>Lowest speed limit among the nodes within a distance ahead (slows down before turns).</summary>
        public float MinLimitWithin(float station, float distance, float fallbackKmh)
        {
            float limit = _nodes.Count > 1 ? _nodes[1].SpeedLimitKmh : fallbackKmh;
            float along = -station;
            for (int i = 1; i < _nodes.Count; i++)
            {
                along += Distance(_nodes[i - 1].Pos, _nodes[i].Pos);
                if (along > distance) break;
                limit = Math.Min(limit, _nodes[i].SpeedLimitKmh);
            }
            return limit;
        }

        /// <summary>First stop line ahead and the distance to it along the path.</summary>
        public bool TryNextStopLine(float station, out CivNode node, out float distance)
        {
            distance = -station;
            for (int i = 1; i < _nodes.Count; i++)
            {
                distance += Distance(_nodes[i - 1].Pos, _nodes[i].Pos);
                if (_nodes[i].IsStopLine) { node = _nodes[i]; return true; }
            }
            node = default;
            return false;
        }

        public static float Distance(RoadPoint a, RoadPoint b) =>
            (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
    }
}
