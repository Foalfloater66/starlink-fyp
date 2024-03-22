using System.Collections.Generic;
using System.Linq;
using Routing;
using UnityEngine;

namespace Orbits
{
    // for ToList();

    /**
 * @author Morgane Marie Ohlig
 */

    /// <summary>
    /// Class <c>Path</c> contains a list of nodes in the path between the <c>end_city</c> and the <c>start_city</c>.
    /// </summary>
    public class Path : HeapNode
    {
        // TODO: move to routing
        /// <value>
        /// List of nodes in the path.
        /// </value>
        public List<Node> nodes = new List<Node>();

        /// <value>
        /// GameObject for the path's origin city.
        /// </value>
        public GameObject start_city;

        /// <value>
        /// GameObject for the path's final destination city.
        /// </value>
        public GameObject end_city;

        /// <summary>
        /// Constructor initializing the <c>Path</c> object and its <c>start_city</c> and the <c>end_city</c>.
        /// </summary>
        /// <param name="start_city">Start city on the path.</param>
        /// <param name="end_city">Start city on the path.</param>
        public Path(GameObject start_city, GameObject end_city)
        {
            this.start_city = start_city;
            this.end_city = end_city;
        }
    }

    /// <summary>
    /// Class <c>Attacker</c> provides a set of properties and methods used to target and flood links in a geographic area of specified center and radius.
    /// </summary>
    public class Attacker
    {
        /// <summary>
        /// Class <c>TargetLink</c> contains information about the link that the <c>Attacker</c> object is aiming towards.
        /// </summary>
        public class TargetLink
        {
            /// <value>
            /// Source node of the target link.
            /// </value>
            public Node SrcNode { get; protected set; }

            /// <value>
            /// Destination node of the target link.
            /// </value>
            public Node DestNode { get; protected set; }

            /// <summary>
            /// Constructor initializing the <c>TargetLink</c> object and its source and destination nodes.
            /// </summary>
            /// <param name="src_node"><c>TargetLink</c> source node.</param>
            /// <param name="dest_node"><c>TargetLink</c> destination node.</param>
            public TargetLink(Node src_node, Node dest_node)
            {
                this.SrcNode = src_node;
                this.DestNode = dest_node;
            }
        }

        /// <summary>
        /// List of source groundstations that the attacker can send packets from.
        /// </summary>
        public List<GameObject> SourceGroundstations { get; set; }

        /// <summary>
        /// Vector3 coordinates of the center of the attack area.
        /// </summary>
        public Vector3 TargetAreaCenterpoint { get; protected set; }

        /// <summary>
        /// Radius of the attack area.
        /// </summary>
        public float Radius { get; protected set; }

        /// <summary>
        /// Link that the <c>Attacker</c> object is targeting.
        /// </summary>
        public TargetLink Link { get; protected set; }

        /// <summary>
        /// File for debugging.
        /// </summary>
        protected System.IO.StreamWriter logfile;

        /// <summary>
        /// Constructor creating an <c>Attacker</c> object with an attack area around the provided <c>(latitude, longitude)</c> coordinates. 
        /// </summary>
        /// <param name="latitude">Latitude of the attack area in degrees.</param>
        /// <param name="longitude">Longitude of the attack area in degrees.</param>
        /// <param name="sat0r">Satellite radius from earth's center.</param>
        /// <param name="attack_radius">Radius of the attack area.</param>
        /// <param name="src_groundstations">List of source groundstations available to the attacker.</param>
        /// <param name="transform">Transform object associated with Earth's center.</param>
        /// <param name="prefab">GameObject representing the attack area center.</param>
        public Attacker(float latitude, float longitude, float sat0r, float attack_radius, List<GameObject> src_groundstations, Transform transform, GameObject prefab)
        {
            this.SourceGroundstations = src_groundstations;
            this.TargetAreaCenterpoint = Vector3.zero;
            this.Radius = attack_radius;
            this.SetTargetArea(latitude, longitude, sat0r, transform, prefab);
            this.Link = null;
            this.logfile = new System.IO.StreamWriter(Main.log_directory + "/Path/attacker_summary.txt");
            UnityEngine.Debug.Log(transform.rotation);
        }

        /// <summary>
        /// Checks if the currently selected target link is still within the target area. 
        /// </summary>
        /// <returns>True if at least one link node is in the target area, false otherwise. If the victim link hasn't been set, returns false.</returns>
        public bool HasValidTargetLink()
        {
            return (this.Link != null && (this.InTargetArea(this.Link.SrcNode.Position) || this.InTargetArea(this.Link.DestNode.Position)));
        }

        /// <summary>
        /// Finds the shortest route between two groundstations.
        /// </summary>
        /// <param name="rg">Built <c>Routegraph</c> object.</param>
        /// <param name="src_gs">Source groundstation.</param>
        /// <param name="dest_gs">Destination groundstation.</param>
        /// <returns>If a route containing the target link is found, creates and returns the route's <c>Path</c>. Otherwise, returns null.</returns>
        public Path FindAttackRoute(RouteGraph rg, GameObject src_gs, GameObject dest_gs)
        {
            // TODO: Optimise route extraction.
            System.Diagnostics.Debug.Assert(this.Link != null, "FindAttackRoute | No attacker link has been set.");

            Node rn = rg.endnode;
            Path route = new Path(src_gs, dest_gs);
            route.nodes.Add(rn);
            int startsatid = 0, endsatid = -1;
            int id = -4;
            int prev_id = -4;
            bool viable_route = false; // can the target link be attacked with this route?

            while (true)
            {
                if (rn == rg.startnode)
                {
                    route.nodes.Add(rn);
                    startsatid = id;
                    break;
                }

                id = rn.Id;

                if (id >= 0)
                {
                    route.nodes.Add(rn);
                }

                if (endsatid == -1 && id >= 0)
                {
                    endsatid = id;
                }

                if (id >= 0 && prev_id >= 0)
                {
                    if (prev_id == this.Link.SrcNode.Id && id == this.Link.DestNode.Id)
                    {
                        viable_route = true;
                    }
                }

                prev_id = id;

                rn = rn.Parent;

                if (rn == null) // this route is incomplete.
                {
                    return null;
                }
            }
            List<string> path = new List<string>();
            foreach (Node node in route.nodes)
            {
                path.Add(node.Id.ToString());
            }

            // TODO: Remove debugging messages.
            if (viable_route)
            {
                UnityEngine.Debug.Log("FindAttackRoute | This is a valid attack route!");
                return route;
            }
            else
            {
                UnityEngine.Debug.Log("FindAttackRoute | Couldn't find a valid attack route.");
            }
            return null;
        }

        /// <summary>
        /// Sets and draws a center for the target area based on provided latitude and longitude coordinates.
        /// </summary>
        /// <param name="latitude">Attack area center latitude coordinates. Must be between -90 and 90 degrees.</param>
        /// <param name="longitude">Attack area center longitude coordinates. Must be between -180 and 180 degrees.</param>
        /// <param name="sat0r">Distance of the satellite from Earth's center.</param>
        /// <param name="transform"><c>Transform</c> object representing the center of the Earth</param>
        /// <param name="prefab">GameObject representing the attack center.</param> 
        protected void SetTargetArea(float latitude, float longitude, float altitude, Transform transform, GameObject prefab)
        {
            System.Diagnostics.Debug.Assert(latitude > -90 && latitude < 90, "Latitude must be between -90 and 90 degrees.");
            System.Diagnostics.Debug.Assert(longitude > -180 && longitude < 180, "Longitude must be between -180 and 180 degrees.");

            // convert from lat, long, and altitude to Vector3 representation.
            GameObject target = GameObject.Instantiate(prefab, new Vector3(0f, 0f, -altitude), transform.rotation);
            float long_offset = 20f;
            target.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
            Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * target.transform.position;
            target.transform.RotateAround(Vector3.zero, lat_axis, latitude);
            target.transform.SetParent(transform, false);

            this.TargetAreaCenterpoint = target.transform.position;
        }

        /// <summary>
        /// Checks if the input coordinates are within the sphere of attack.
        /// </summary>
        /// <param name="position">Coordinates.</param>
        /// <returns>Returns true if the coordinates are within the area, and false otherwise.</returns>
        protected bool InTargetArea(Vector3 position)
        {
            return (Vector3.Distance(position, this.TargetAreaCenterpoint) < this.Radius);
        }

        /// <summary>
        /// Searches for a random node in the routegraph that is in the target area.
        /// </summary>
        /// <param name="rg">Built <c>Routegraph</c> object.</param>
        /// <returns>If a valid node was found, returns it. Otherwise, returns null.</returns>
        protected Node SelectSrcNode(RouteGraph rg)
        {
            Node node = null;
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
                if (node.Id > 0 && this.InTargetArea(node.Position))
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
        /// Searches for a random node linked to the <c>src_node</c>.
        /// </summary>
        /// <param name="src_node">Potential target link source node.</param>
        /// <returns>If a valid node was found, returns it. Otherwise, returns null.</returns>
        protected Node SelectOutOfTargetDestinationNode(Node src_node)
        {
            Node node = null;
            HashSet<int> explored_indexes = new HashSet<int>();
            int j = 0;

            while (j < src_node.LinkCount)
            {
                int i = new System.Random().Next(src_node.LinkCount);
                if (explored_indexes.Contains(i))
                {
                    continue;
                }
                node = src_node.GetNeighbour(src_node.GetLink(i));
                if (node.Id > 0)
                {
                    break;
                }
                explored_indexes.Add(i);
                j++;
            }

            return (node != null && node.Id > 0 ? node : null); // only return valid destination nodes.
        }

        /// <summary>
        /// Searches for a random node linked to the <c>src_node</c> that is within the target area.
        /// </summary>
        /// <param name="src_node">Potential target link source node.</param>
        /// <returns>A valid node if one is found. Otherwise, returns null.</returns>
        protected Node SelectInTargetDestinationNode(Node src_node)
        {
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
                if (node.Id > 0 && this.InTargetArea(node.Position))
                {
                    return node;
                }
                nodes[i] = null;
                remaining_nodes -= 1;
            }
            return null;
        }

        /// <summary>
        /// Searches for a random destination node connected to the <c>src_node</c> to create a valid target link, prioritising destination nodes that are within the target area.
        /// </summary>
        /// <param name="src_node">Potential target link source node.</param>
        /// <returns>A valid node if one is found. Otherwise, returns null.</returns>
        protected Node SelectDestinationNode(Node src_node)
        {
            Node dest_node = this.SelectInTargetDestinationNode(src_node);
            if (dest_node == null)
            {
                return this.SelectOutOfTargetDestinationNode(src_node); // select random link which is partially in the target radius
            }
            return dest_node;
        }

        /// <summary>
        /// Searches for a target link within the target area. If successful, sets the property <c>Link</c> to a new <c>Attacker.TargetLink</c> object with the new <c>src_node</c> and <c>dest_node</c> nodes. Otherwise, sets the property <c>Link</c> to null. Failure may occur if no source node was found or if the source node has no links.
        /// </summary>
        /// <param name="rg">Built <c>Routegraph</c> object.</param>
        public void ChangeTargetLink(RouteGraph rg)
        {
            Node src_node = this.SelectSrcNode(rg);
            if (src_node != null)
            {
                Node dest_node = this.SelectDestinationNode(src_node);
                if (dest_node != null)
                {
                    this.Link = new Attacker.TargetLink(src_node, dest_node);
                    return;
                }
            }
            this.Link = null;
        }

    }

    /// <summary>
    /// Class <c>DebugAttacker</c>, subclass of the <c>Attacker</c> class does not have randomized link selection. Useful for debugging or proof of concept.
    /// </summary>
    public class DebugAttacker : Attacker
    {

        /// <summary>
        /// Constructor initializing a <c>DebugAttacker</c> object with an attack area around the provided <c>(latitude, longitude)</c> coordinates. 
        /// </summary>
        /// <param name="latitude">Latitude of the attack area in degrees.</param>
        /// <param name="longitude">Longitude of the attack area in degrees.</param>
        /// <param name="sat0r">Satellite radius from earth's center.</param>
        /// <param name="attack_radius">Radius of the attack area.</param>
        /// <param name="src_groundstations">List of source groundstations available to the attacker.</param>
        /// <param name="transform">Transform object associated with Earth's center.</param>
        /// <param name="prefab">GameObject representing the attack area center.</param>
        public DebugAttacker(float latitude, float longitude, float sat0r, float attack_radius, List<GameObject> src_groundstations, Transform transform, GameObject prefab) : base(latitude, longitude, sat0r, attack_radius, src_groundstations, transform, prefab)
        {

        }

        /// <summary>
        /// Selects the first node in the routegraph that is in the target area.
        /// </summary>
        /// <param name="rg">Built <c>Routegraph</c> object.</param>
        /// <returns>If a valid node was found, returns it. Otherwise, returns null.</returns>
        private Node SelectSrcNode(RouteGraph rg)
        {
            int remaining_nodes = rg.nodes.Count();

            for (int i = 0; i < rg.nodes.Count(); i++)
            {
                Node node = rg.nodes[i];
                if (node.Id > 0 && this.InTargetArea(node.Position))
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Selects the first node linked to the <c>src_node</c>.
        /// </summary>
        /// <param name="src_node">Candidate source node.</param>
        /// <returns>A valid node if one is found. Otherwise, returns null.</returns>
        private Node SelectOutOfTargetDestinationNode(Node src_node)
        {
            for (int i = 0; i < src_node.LinkCount; i++)
            {
                Node node = src_node.GetNeighbour(src_node.GetLink(i));
                if (node.Id > 0)
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Selects the first node linked to the <c>src_node</c> that is within the target area.
        /// </summary>
        /// <param name="src_node">Candidate source node.</param>
        /// <returns>A valid node if one is found. Otherwise, returns null.</returns>
        private Node SelectInTargetDestinationNode(Node src_node)
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

    }

    /// <summary>
    /// Static class <c>AttackerFactory</c> provides the method <c>CreateAttacker</c> to either create an <c>Attacker</c> or a <c>DebugAttacker</c> class.
    /// </summary>
    public static class AttackerFactory
    {
        /// <summary>
        /// Creates an <c>Attacker</c>/<c>DebugAttacker</c> object based on whether the <c>debug</c> option is enabled or not.
        /// </summary>
        /// <param name="latitude">Latitude of the attack area in degrees.</param>
        /// <param name="longitude">Longitude of the attack area in degrees.</param>
        /// <param name="sat0r">Satellite radius from earth's center.</param>
        /// <param name="attack_radius">Radius of the attack area.</param>
        /// <param name="src_groundstations">List of source groundstations available to the attacker.</param>
        /// <param name="transform">Transform object associated with Earth's center.</param>
        /// <param name="prefab">GameObject representing the attack area center.</param>
        /// <param name="debug">Debugging option. If enabled, a <c>DebugAttacker</c> object is returned. Otherwise, an <c>Attacker</c> object is created.</param>
        /// <returns>An instance of a <c>Attacker</c>/<c>DebugAttacker</c> object.</returns>
        public static Attacker CreateAttacker(float latitude, float longitude, float sat0r, float attack_radius, List<GameObject> src_groundstations, Transform transform, GameObject prefab, bool debug)
        {
            if (debug)
            {
                return new DebugAttacker(latitude, longitude, sat0r, attack_radius, src_groundstations, transform, prefab);
            }
            else
            {
                return new Attacker(latitude, longitude, sat0r, attack_radius, src_groundstations, transform, prefab);
            }

        }
    }
}