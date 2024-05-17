using System.Collections.Generic;
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

        public Path(Node startNode, Node endNode, GameObject startCity, GameObject endCity)
        {
            StartNode = startNode;
            EndNode = endNode;
            StartCity = startCity;
            EndCity = endCity;
        }
    }
}