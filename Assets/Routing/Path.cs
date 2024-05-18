using System.Collections.Generic;
using System.Xml.Serialization.Configuration;
using UnityEngine;
using Utilities;

namespace Routing
{
    /// <summary>
    /// Class <c>Path</c> contains a list of nodes in the path between the <c>end_city</c> and the <c>start_city</c>.
    /// </summary>
    public class Path : HeapNode
    {
        public readonly List<Node> Nodes = new List<Node>();
        public readonly Node StartNode;
        public readonly Node EndNode;
        public readonly GameObject StartCity;
        public readonly GameObject EndCity;
        public readonly int Load;
        public readonly float Dist; // Save the distance between start node and end node

        public Path(Node startNode, Node endNode, GameObject startCity, GameObject endCity, int load)
        {
            StartNode = startNode;
            EndNode = endNode;
            Dist = endNode.Dist;
            StartCity = startCity;
            EndCity = endCity;
            Load = load;
        }

        // Get RTT in seconds
        public float GetRTT(float kmPerUnit)
        {
            var endDist = kmPerUnit * Dist;
            return 2 * endDist / 299.792f; // speed of light in a vacuum  (metres/second)
        }

        // public static Path ToPath()
        // {
        //     
        // }
    }
}