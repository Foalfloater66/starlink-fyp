using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Routing;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;

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

        public Vector3 Center { get; set; }

        public float Radius { get; set; }

        public TargetLink Link { get; private set; }

        private AttackParams _attackParams;

        // private 
        public readonly int OrbitId; // If the orbit Id is -1, then the target link is not restricted by its orbit.

        public AttackTarget(AttackParams attackParams, float sat0r, float radius, Transform transform,
            GameObject prefab)
        {
            Link = null;
            Center = Vector3.zero;
            Radius = radius;
            OrbitId = attackParams.OrbitId;
            
            _attackParams = attackParams;
            SetTargetArea(attackParams.Latitude, attackParams.Longitude, sat0r, transform, prefab);
        }

        /// <summary>
        /// Sets and draws a center for the target area based on provided latitude and longitude coordinates.
        /// </summary>
        /// <param name="latitude">Attack area center latitude coordinates. Must be between -90 and 90 degrees.</param>
        /// <param name="longitude">Attack area center longitude coordinates. Must be between -180 and 180 degrees.</param>
        /// <param name="altitude">Altitude of satellites from Earth's surface.</param>
        /// <param name="transform"><c>Transform</c> object representing the center of the Earth</param>
        /// <param name="prefab">GameObject representing the attack center.</param> 
        private void SetTargetArea(float latitude, float longitude, float altitude, Transform transform,
            GameObject prefab)
        {
            System.Diagnostics.Debug.Assert(latitude > -90 && latitude < 90);
            System.Diagnostics.Debug.Assert(longitude > -180 && longitude < 180);

            // convert from lat, long, and altitude to Vector3 representation.
            var target = Object.Instantiate(prefab, new Vector3(0f, 0f, -altitude), transform.rotation);
            var long_offset = 20f;
            target.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
            var lat_axis = Quaternion.Euler(0f, -90f, 0f) * target.transform.position;
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
            return Link != null && InTargetArea(Link.SrcNode.Position) && InTargetArea(Link.DestNode.Position);
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
        /// Checks if the target link is of the desired orientation.
        /// </summary>
        /// <param name="srcPosition">Source node position.</param>
        /// <param name="destPosition">Destination node position.</param>
        /// <returns></returns>
        private bool IsHorizontal(Node srcNode, Node destNode) //srcPosition, Vector3 destPosition)
        {
            var srcPosition = srcNode.Position;
            var destPosition = destNode.Position;
            var northVector = new Vector3(0.0f, 1.0f, 0.0f); // Reference unit northern vector
            var candidateVector = destPosition - srcPosition; // Candidate vector
            var angle = Vector3.Angle(northVector, candidateVector);
            if (45 < angle && angle < 135) return true; // link is horizontally inclined.
            return false; // link is vertically inclined.
        }

        private bool IsDirectedNorth(Node srcNode, Node destNode)
        {
            var srcPosition = srcNode.Position;
            var destPosition = destNode.Position;
            var candidateVector = (destPosition - srcPosition).normalized;
            if (candidateVector.y >= 0) return true;
            return false;
        }

        private bool IsDirectedEast(Node srcNode, Node destNode)
        {
            var candidateVector = (destNode.Position - srcNode.Position).normalized;
            if (candidateVector.x >= 0) return true;
            return false;
        }

        private Vector3 GetLinkDirection(Node srcNode, Node destNode)
        {
            return (destNode.Position - srcNode.Position).normalized;
        }

        /// <summary>
        /// Searches for a random node in the routegraph that is in the target area.
        /// </summary>
        /// <param name="rg">Built <c>Routegraph</c> object.</param>
        /// <param name="debug_on">If set to true, selects the first node that satisfy target node criterion instead. Useful for debugging.</param>
        /// <returns>If a valid node was found, returns it. Otherwise, returns null.</returns>
        private Tuple<Node, Node> SelectLink(RouteGraph rg, bool debug_on)
        {
            for (var i = 0; i < rg.nodes.Count(); i++)
            {
                Node node = rg.nodes[i];
                if (OrbitId != -1 && OrbitId != node.Orbit)
                    // If orbit-specific links are enabled, exclude links that are not part of the desired orbit.
                    continue;
                if (node.Id > 0 && InTargetArea(node.Position))
                {
                    var link = SelectDestinationNode(node, debug_on);
                    if (link == null) continue;
                    return link;
                }
            }
            return null;
            // }

            // // this is the randomization code. It's never used. I should be able to remove it.
            // Dictionary<int, Node> nodes = new Dictionary<int, Node>(); // index, node 
            // int remaining_nodes = rg.nodes.Count();
            // for (int i = 0; i < rg.nodes.Count(); i++) nodes.Add(i, rg.nodes[i]);
            //
            // while (remaining_nodes > 0)
            // {
            //     int i = new System.Random().Next(rg.nodes.Count());
            //     node = nodes[i];
            //     if (node == null)
            //     {
            //         continue; // already seen.
            //     }
            //
            //     if (node.Id > 0 && InTargetArea(node.Position))
            //     {
            //         // src_node = node;
            //         break;
            //     }
            //
            //     nodes[i] = null;
            //     remaining_nodes -= 1;
            // }
            //
            // return node; // the node might not have any links!
        }

        /// <summary>
        /// Searches for a random node linked to the <c>src_node</c> that is within the target area.
        /// </summary>
        /// <param name="src_node">Potential target link source node.</param>
        /// <param name="debug_on">If set to true, selects the first node that satisfy target node criterion instead. Useful for debugging.</param>
        /// <returns>A valid node if one is found. Otherwise, returns null.</returns>
        private Tuple<Node, Node> SelectDestinationNode(Node src_node, bool debug_on)
        {
            // if (debug_on)ds
            // {
            for (var i = 0; i < src_node.LinkCount; i++)
            {
                var node = src_node.GetNeighbour(src_node.GetLink(i));
                if (OrbitId != -1 && OrbitId != node.Orbit)
                    // If orbit-specific links are enabled, exclude links that are not part of the desired orbit.
                    continue;
                if (node.Id > 0 && InTargetArea(node.Position))
                {
                    var direction = GetLinkDirection(src_node, node);
                    if (OrbitId == -1) // If the orbit is not specified, switch the orientation!
                    {
                        if (new HashSet<Direction>{ Direction.East, Direction.West }.Contains(_attackParams.Direction) &&
                            !IsHorizontal(src_node, node))
                        {
                            continue;
                        }
                        else if (new HashSet<Direction>
                                { Direction.South, Direction.North }.Contains(_attackParams.Direction) &&
                            IsHorizontal(src_node, node))
                        {
                            continue;
                        }
                    }
                    if (_attackParams.Direction == Direction.Any
                        // TODO: make sure that the horizontal/vertical variation DOES NOT occur
                        || (_attackParams.Direction == Direction.East && direction.x >= 0) // && IsHorizontal(src_node, node))
                        || (_attackParams.Direction == Direction.West && direction.x < 0) // && IsHorizontal(src_node, node))
                        || (_attackParams.Direction == Direction.North && direction.y >= 0) // && !IsHorizontal(src_node, node))
                        || (_attackParams.Direction == Direction.South && direction.y < 0)) //  && !IsHorizontal(src_node, node)))
                    {
                        return new Tuple<Node, Node>(src_node, node);
                    }
                }
            }

            return null;
            // }

            // FIXME: I think this function never gets used. I have to decide whether to keep it or not.
            // Dictionary<int, Node> nodes = new Dictionary<int, Node>(); // index, node 
            // int remaining_nodes = src_node.LinkCount;
            //
            // for (int i = 0; i < src_node.LinkCount; i++) nodes.Add(i, src_node.GetNeighbour(src_node.GetLink(i)));
            //
            // while (remaining_nodes > 0) // prioritise ISL links that are fully contained in the target radius.
            // {
            //     int i = new System.Random().Next(src_node.LinkCount);
            //     Node node = nodes[i];
            //     if (node == null)
            //     {
            //         continue;
            //     }
            //
            //     if (node.Id > 0 && InTargetArea(node.Position))
            //     {
            //         return node;
            //     }
            //
            //     nodes[i] = null;
            //     remaining_nodes -= 1;
            // }
            // return null;
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
            var nodes = SelectLink(rg, debug_on);

            if (nodes != null)
                Link = new TargetLink(nodes.Item1, nodes.Item2);
            else
                Link = null;
        }
    }
}