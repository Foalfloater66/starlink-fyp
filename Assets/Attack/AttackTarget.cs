using System.Collections.Generic;
using System.Linq;
using Routing;
using UnityEngine;

namespace Attack
{
    public class AttackTarget
    {
        /// <summary>
        /// Class <c>TargetLink</c> contains information about the link that the <c>Attacker</c> object is aiming towards.
        /// </summary>
        public class TargetLink
        {
            /// <value>
            /// Source node of the target link.
            /// </value>
            public Node SrcNode { get; private set; }

            /// <value>
            /// Destination node of the target link.
            /// </value>
            public Node DestNode { get; private set; }

            /// <summary>
            /// Constructor initializing the <c>TargetLink</c> object and its source and destination nodes.
            /// </summary>
            /// <param name="src_node"><c>TargetLink</c> source node.</param>
            /// <param name="dest_node"><c>TargetLink</c> destination node.</param>
            public TargetLink(Node src_node, Node dest_node)
            {
                SrcNode = src_node;
                DestNode = dest_node;
            }
        }

        private Vector3 Center { get; set; }

        private float Radius { get; set; }

        public TargetLink Link { get; private set; }

        public AttackTarget(float latitude, float longitude, float sat0r, float radius, Transform transform,
            GameObject prefab)
        {
            Link = null;
            Center = Vector3.zero;
            Radius = radius;
            SetTargetArea(latitude, longitude, sat0r, transform, prefab);
        }


        /// <summary>
        /// Sets and draws a center for the target area based on provided latitude and longitude coordinates.
        /// </summary>
        /// <param name="latitude">Attack area center latitude coordinates. Must be between -90 and 90 degrees.</param>
        /// <param name="longitude">Attack area center longitude coordinates. Must be between -180 and 180 degrees.</param>
        /// <param name="sat0r">Distance of the satellite from Earth's center.</param>
        /// <param name="altitude">Altitude of satellites from Earth's surface.</param>
        /// <param name="transform"><c>Transform</c> object representing the center of the Earth</param>
        /// <param name="prefab">GameObject representing the attack center.</param> 
        private void SetTargetArea(float latitude, float longitude, float altitude, Transform transform,
            GameObject prefab)
        {
            System.Diagnostics.Debug.Assert(latitude > -90 && latitude < 90);
            System.Diagnostics.Debug.Assert(longitude > -180 && longitude < 180);

            // convert from lat, long, and altitude to Vector3 representation.
            GameObject target = Object.Instantiate(prefab, new Vector3(0f, 0f, -altitude), transform.rotation);
            float long_offset = 20f;
            target.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
            Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * target.transform.position;
            target.transform.RotateAround(Vector3.zero, lat_axis, latitude);
            target.transform.SetParent(transform, false);

            Center = target.transform.position;
        }

        /// <summary>
        /// Checks if the currently selected target link is still within the target area. 
        /// </summary>
        /// <returns>True if both of the target link's nodes are in the target area, false otherwise. If the victim link hasn't been set, returns false.</returns>
        public bool HasValidTargetLink()
        {
            return Link != null && (InTargetArea(Link.SrcNode.Position) && InTargetArea(Link.DestNode.Position));
        }
        
        /// <summary>
        /// Checks if the input coordinates are within the sphere of attack.
        /// </summary>
        /// <param name="position">Coordinates.</param>
        /// <returns>Returns true if the coordinates are within the area, and false otherwise.</returns>
        private bool InTargetArea(Vector3 position)
        {
            return Vector3.Distance(position, Center) < Radius;
        }

        /// <summary>
        /// Searches for a random node in the routegraph that is in the target area.
        /// </summary>
        /// <param name="rg">Built <c>Routegraph</c> object.</param>
        /// <param name="debug_on">If set to true, selects the first node that satisfy target node criterion instead. Useful for debugging.</param>
        /// <returns>If a valid node was found, returns it. Otherwise, returns null.</returns>
        private Node SelectSrcNode(RouteGraph rg, bool debug_on)
        {
            Node node = null;
            if (debug_on)
            {
                for (int i = 0; i < rg.nodes.Count(); i++)
                {
                    node = rg.nodes[i];
                    if (node.Id > 0 && InTargetArea(node.Position))
                    {
                        return node;
                    }
                }

                return null;
            }

            Dictionary<int, Node> nodes = new Dictionary<int, Node>(); // index, node 
            int remaining_nodes = rg.nodes.Count();
            for (int i = 0; i < rg.nodes.Count(); i++) nodes.Add(i, rg.nodes[i]);

            while (remaining_nodes > 0)
            {
                int i = new System.Random().Next(rg.nodes.Count());
                node = nodes[i];
                if (node == null)
                {
                    continue; // already seen.
                }

                if (node.Id > 0 && InTargetArea(node.Position))
                {
                    // src_node = node;
                    break;
                }

                nodes[i] = null;
                remaining_nodes -= 1;
            }

            return node; // the node might not have any links!
        }

        /// <summary>
        /// Searches for a random node linked to the <c>src_node</c> that is within the target area.
        /// </summary>
        /// <param name="src_node">Potential target link source node.</param>
        /// <param name="debug_on">If set to true, selects the first node that satisfy target node criterion instead. Useful for debugging.</param></param>
        /// <returns>A valid node if one is found. Otherwise, returns null.</returns>
        private Node SelectDestinationNode(Node src_node, bool debug_on)
        {
            if (debug_on)
            {
                for (int i = 0; i < src_node.LinkCount; i++)
                {
                    Node node = src_node.GetNeighbour(src_node.GetLink(i));
                    if (node.Id > 0 && this.InTargetArea(node.Position))
                    {
                        return node;
                    }
                }

                return null;
            }

            // FIXME: I think this function never gets used. I have to decide whether to keep it or not.
            Dictionary<int, Node> nodes = new Dictionary<int, Node>(); // index, node 
            int remaining_nodes = src_node.LinkCount;

            for (int i = 0; i < src_node.LinkCount; i++) nodes.Add(i, src_node.GetNeighbour(src_node.GetLink(i)));

            while (remaining_nodes > 0) // prioritise ISL links that are fully contained in the target radius.
            {
                int i = new System.Random().Next(src_node.LinkCount);
                Node node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (node.Id > 0 && InTargetArea(node.Position))
                {
                    return node;
                }

                nodes[i] = null;
                remaining_nodes -= 1;
            }
            return null;
        }

        /// <summary>
        /// Searches for a target link within the target area. If successful, sets the property <c>Link</c>
        /// to a new <c>Attacker.TargetLink</c> object with the new <c>src_node</c> and <c>dest_node</c> nodes.
        /// Otherwise, sets the property <c>Link</c> to null. Failure may occur if no source node was found
        /// or if the source node has no links.
        /// </summary>
        /// <param name="rg">Built <c>Routegraph</c> object.</param>
        /// <param name="debug_on">If set to true, selects the first link that satisfy target link criterion. Useful for debugging.</param>
        public void ChangeTargetLink(RouteGraph rg, bool debug_on)
        {
            Node src_node = SelectSrcNode(rg, debug_on);

            if (src_node != null)
            {
                Node dest_node = SelectDestinationNode(src_node, debug_on);
                if (dest_node != null)
                {
                    Link = new TargetLink(src_node, dest_node);
                    return;
                }
            }

            Link = null;
        }
    }
}