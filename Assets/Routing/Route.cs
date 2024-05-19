using System.Collections.Generic;
using Attack;
using UnityEngine;
using Utilities;

namespace Routing
{
    /// <summary>
    /// Class <c>Path</c> contains a list of nodes in the path between the <c>end_city</c> and the <c>start_city</c>.
    /// </summary>
    public class Route : HeapNode
    {
        public readonly List<Node> Nodes = new List<Node>(); // Nodes are stored in *reverse order* (end first, start last)
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
            // Iterate through path in reverse order (end to start)
            for (int idx = 0; idx < Nodes.Count; idx++)
            {
                if (idx == 0)
                {
                    continue;
                }
                Node node = Nodes[idx];
                Node nextNode = Nodes[idx - 1];
                if (node.Id == srcNodeId && nextNode.Id == destNodeId)
                    return true;
            }
            return false;
        }
        
        public bool HasEarlyCollisions(int desiredMbits,
            LinkCapacityMonitor linkCapacities, string startCityName, AttackTarget.TargetLink link)
        {
            // Iterate through path in normal order (start to end)
            for (int idx = Nodes.Count - 2; idx >= 0; idx--)
            {
                // We don't check the very last link, because this code only has ISLs as targets.
                if (idx == 0)
                    break;
                Node prevRn = Nodes[idx + 1];
                Node rn = Nodes[idx];
                if (prevRn == link.SrcNode && rn == link.DestNode) break;
                if (idx == Nodes.Count - 2) // RF link (startNode - satellite)
                {
                    if (linkCapacities.GetCapacity(startCityName, rn.Id.ToString()) - desiredMbits < 0) 
                        return true;
                } 
                else if (linkCapacities.GetCapacity(prevRn.Id, rn.Id) - desiredMbits < 0) // ISL link.
                {
                    return true;
                }
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
