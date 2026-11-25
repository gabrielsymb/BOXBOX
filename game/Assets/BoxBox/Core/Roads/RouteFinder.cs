using System;
using System.Collections.Generic;

namespace BoxBox.Roads
{
    /// <summary>
    /// Junction-to-junction route search ported from GTA2 (gta2_re, Source/RouteFinder.cpp):
    /// NewChildNode_5892F0, InsertIntoOpenList_589420, ExpandNode_5899C0, SearchToDest_589E20.
    /// Kept faithful, quirks included:
    /// - a node's cost is parent cost + link length + Manhattan distance to the goal, and the parent
    ///   cost already holds the parent's own distance term (greedy best-first, not strict A*);
    /// - a junction is marked visited when it is inserted, not when it is expanded;
    /// - the search stops as soon as the goal is inserted;
    /// - exits are tried in the order N, S, W, E.
    /// Links can be closed (GTA2 RoadOff_588810) for roadworks or blocked streets.
    /// </summary>
    public sealed class RouteFinder
    {
        private static readonly RoadExit[] ExpandOrder = { RoadExit.North, RoadExit.South, RoadExit.West, RoadExit.East };

        private sealed class Node
        {
            public int Junction;
            public float Cost;
            public bool Expanded;
            public Node Parent;
        }

        private readonly RoadNetwork _network;
        private readonly HashSet<int> _closedStraights = new HashSet<int>();

        public RouteFinder(RoadNetwork network) => _network = network ?? throw new ArgumentNullException(nameof(network));

        public void SetStraightOpen(RoadSurfaceId straight, bool open)
        {
            if (open) _closedStraights.Remove(straight.Index);
            else _closedStraights.Add(straight.Index);
        }

        /// <summary>Junctions from start to goal, both included; empty when unreachable.</summary>
        public List<RoadSurfaceId> FindRoute(RoadSurfaceId start, RoadSurfaceId goal)
        {
            var route = new List<RoadSurfaceId>();
            if (start.Kind != RoadSurfaceKind.Junction || goal.Kind != RoadSurfaceKind.Junction) return route;

            var visited = new bool[_network.Junctions.Count];
            var open = new List<Node>();                                // sorted by cost, like the linked list
            var first = new Node { Junction = start.Index };
            Insert(open, visited, first);

            Node found = start.Index == goal.Index ? first : null;
            while (found == null)
            {
                Node node = open.Find(n => !n.Expanded);                // GetFirstUnexpandedNode_589E00
                if (node == null) return route;
                found = Expand(node, goal.Index, open, visited);
            }

            for (Node n = found; n != null; n = n.Parent)
                route.Add(new RoadSurfaceId(RoadSurfaceKind.Junction, n.Junction));
            route.Reverse();
            return route;
        }

        /// <summary>Nearest junction to a point, for starting or ending a route off the graph.</summary>
        public RoadSurfaceId NearestJunction(RoadPoint p)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < _network.Junctions.Count; i++)
            {
                RoadPoint c = _network.Junctions[i].Centre;
                float d = (c.X - p.X) * (c.X - p.X) + (c.Z - p.Z) * (c.Z - p.Z);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best < 0 ? RoadSurfaceId.None : new RoadSurfaceId(RoadSurfaceKind.Junction, best);
        }

        private Node Expand(Node node, int goal, List<Node> open, bool[] visited)
        {
            node.Expanded = true;
            RoadJunction junction = _network.Junctions[node.Junction];
            foreach (RoadExit exit in ExpandOrder)
            {
                RoadSurfaceId link = junction.Exits[(int)exit];
                if (link.Kind != RoadSurfaceKind.Straight || _closedStraights.Contains(link.Index)) continue;

                RoadStraight street = _network.Straights[link.Index];
                RoadSurfaceId other = OtherEnd(street, node.Junction);
                if (other.Kind != RoadSurfaceKind.Junction || visited[other.Index]) continue;

                var child = new Node
                {
                    Junction = other.Index,
                    Cost = node.Cost + street.LengthM + Manhattan(other.Index, goal),
                    Parent = node
                };
                Insert(open, visited, child);
                if (child.Junction == goal) return child;
            }
            return null;
        }

        private static RoadSurfaceId OtherEnd(RoadStraight street, int junction)
        {
            foreach (RoadSurfaceId end in street.Connect)
                if (end.Kind == RoadSurfaceKind.Junction && end.Index != junction) return end;
            return RoadSurfaceId.None;
        }

        private float Manhattan(int a, int b)
        {
            RoadPoint pa = _network.Junctions[a].Centre, pb = _network.Junctions[b].Centre;
            return Math.Abs(pa.X - pb.X) + Math.Abs(pa.Z - pb.Z);
        }

        // InsertIntoOpenList_589420: mark visited, insert after every node that costs strictly less.
        private static void Insert(List<Node> open, bool[] visited, Node node)
        {
            visited[node.Junction] = true;
            int i = 0;
            while (i < open.Count && open[i].Cost < node.Cost) i++;
            open.Insert(i, node);
        }
    }
}
