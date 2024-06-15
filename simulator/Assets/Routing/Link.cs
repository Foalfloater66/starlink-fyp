using UnityEngine;

namespace Routing
{
    public class Link
    {
        private Node[] nodes;
        private float dist; // length of link
        public float Dist { get; set; }
        private bool dist_limited; // RF links have a distance limit, lasers do not

        public Link(Node n1, Node n2, bool dist_limited_)
        {
            nodes = new Node[2];
            nodes[0] = n1;
            nodes[1] = n2;
            Dist = Vector3.Distance(n1._position, n2._position);
            dist_limited = dist_limited_;
        }

        public Link(Node n1, Node n2, float dist_, bool dist_limited_)
        {
            nodes = new Node[2];
            nodes[0] = n1;
            nodes[1] = n2;
            Dist = dist_;
            dist_limited = dist_limited_;
        }

        public Node OtherNode(Node n)
        {
            if (nodes[0] == n) return nodes[1];
            Debug.Assert(nodes[1] == n);
            return nodes[0];
        }

        public void UpdateDist(float maxdist)
        {
            Dist = Vector3.Distance(nodes[0]._position, nodes[1]._position);
            if (dist_limited && Dist > maxdist) Dist = Node.INFINITY; // unreachable
        }
    }
}