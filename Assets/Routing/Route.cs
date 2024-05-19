using System;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Routing
{
    /// <summary>
    /// Class <c>Path</c> contains a list of nodes in the path between the <c>end_city</c> and the <c>start_city</c>.
    /// </summary>
    public class Route : HeapNode
    {
        public readonly List<Node> Nodes = new List<Node>();
        public readonly Node StartNode;
        public readonly Node EndNode;
        public readonly GameObject StartCity;
        public readonly GameObject EndCity;
        
        private readonly float _dist; // Save the distance between start node and end node

        private Route(Node startNode, Node endNode, GameObject startCity, GameObject endCity)
        {
            // Create a route from existing nodes.
            _dist = endNode.Dist;
            StartNode = startNode;
            EndNode = endNode;
            StartCity = startCity;
            EndCity = endCity;
        }
        
        // Get RTT in seconds
        public float GetRTT(float kmPerUnit)
        {
            var endDist = kmPerUnit * _dist;
            return 2 * endDist / 299.792f; // speed of light in a vacuum  (metres/second)
        }

        public bool ContainsLink(int srcNodeId, int destNodeId)
        {
            for (int idx = 0; idx < Nodes.Count; idx++)
            {
                if (idx == 0)
                {
                    continue;
                }
                Node node = Nodes[idx];
                Node prevNode = Nodes[idx - 1];
                if (prevNode.Id == destNodeId && node.Id == srcNodeId)
                    return true;
            }
            return false;
        }

        public static Route Nodes2Route(Node startNode, Node endNode, GameObject srcGs, GameObject destGs)
        {
            var rn = endNode;
            var route = new Route(startNode, endNode, srcGs, destGs);
            route.Nodes.Add(rn);
            var endsatid = -1;
            var id = -4;
            while (true)
            {
                if (rn == startNode)
                {
                    route.Nodes.Add(rn);
                    break;
                }
                id = rn.Id;
                if (id >= 0) route.Nodes.Add(rn);
                if (endsatid == -1 && id >= 0) endsatid = id;
                rn = rn.Parent;
                if (rn == null) // this route is incomplete.
                    return null;
            }
            return route;
        }
    }
}