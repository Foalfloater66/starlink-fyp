using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Orbits;
using Orbits.Satellites;
using Routing;
using Scene;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;
using Object = UnityEngine.Object;

namespace Attack
{
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
        public readonly List<Node> nodes = new List<Node>();

        /// <value>
        /// GameObject for the path's origin city.
        /// </value>
        public readonly GameObject start_city;

        /// <value>
        /// GameObject for the path's final destination city.
        /// </value>
        public readonly GameObject end_city;

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
        public Vector3 TargetAreaCenterpoint { get; private set; }

        /// <summary>
        /// Radius of the attack area.
        /// </summary>
        public float Radius { get; private set; }

        /// <summary>
        /// Link that the <c>Attacker</c> object is targeting.
        /// </summary>
        public TargetLink Link { get; private set; }

        private RouteHandler _RouteHandler;

        private GroundstationCollection _Groundstations;

        /// <summary>
        /// File for debugging.
        /// </summary>
        private System.IO.StreamWriter logfile;

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
        public Attacker(float latitude, float longitude, float sat0r, float attack_radius,
            List<GameObject> src_groundstations, Transform transform, GameObject prefab, GroundstationCollection groundstations, RouteHandler route_handler)
        { // REVIEW: I can create an attackbuilder object? Or more like a context packet to supply.
            SourceGroundstations = src_groundstations; // TODO: should this also be a groundstation collection object?
            TargetAreaCenterpoint = Vector3.zero;
            Radius = attack_radius;
            _RouteHandler = route_handler;
            _Groundstations = groundstations;
            SetTargetArea(latitude, longitude, sat0r, transform, prefab);
            Link = null;
            logfile = new System.IO.StreamWriter(Main.log_directory + "/Path/attacker_summary.txt");
            Debug.Log(transform.rotation);
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
        /// Finds the shortest route between two groundstations.
        /// </summary>
        /// <param name="start_node"></param>
        /// <param name="end_node"></param>
        /// <param name="src_gs">Source groundstation.</param>
        /// <param name="dest_gs">Destination groundstation.</param>
        /// <returns>If a route containing the target link is found, creates and returns the route's <c>Path</c>. Otherwise, returns null.</returns>
        public Path FindAttackRoute(Node start_node, Node end_node, GameObject src_gs, GameObject dest_gs)
        {
            // TODO: Optimise route extraction.
            System.Diagnostics.Debug.Assert(this.Link != null, "FindAttackRoute | No attacker link has been set.");

            Node rn = end_node;
            Path route = new Path(src_gs, dest_gs);
            route.nodes.Add(rn);
            int startsatid = 0, endsatid = -1;
            int id = -4;
            int prev_id = -4;
            bool viable_route = false; // can the target link be attacked with this route?

            while (true)
            {
                if (rn == start_node)
                {
                    route.nodes.Add(rn);
                    startsatid = id; // TODO: do I need to keep this?
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

            // TODO: Remove debugging messages.
            if (viable_route)
            {
                UnityEngine.Debug.Log("FindAttackRoute | This is a valid attack route!");
                return route;
            }
            UnityEngine.Debug.Log("FindAttackRoute | Couldn't find a valid attack route.");
            return null;
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
            System.Diagnostics.Debug.Assert(latitude > -90 && latitude < 90,
                "Latitude must be between -90 and 90 degrees.");
            System.Diagnostics.Debug.Assert(longitude > -180 && longitude < 180,
                "Longitude must be between -180 and 180 degrees.");

            // convert from lat, long, and altitude to Vector3 representation.
            GameObject target = Object.Instantiate(prefab, new Vector3(0f, 0f, -altitude), transform.rotation);
            float long_offset = 20f;
            target.transform.RotateAround(Vector3.zero, Vector3.up, longitude - long_offset);
            Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * target.transform.position;
            target.transform.RotateAround(Vector3.zero, lat_axis, latitude);
            target.transform.SetParent(transform, false);
            
            // Check if the cityScript is attached.
            Component[] components = target.GetComponents<Component>();
            foreach (Component component in components)
            {
                Debug.Log(component.GetType().ToString());
            }
            Debug.Log("Tried printing all scripts.");
            
            TargetAreaCenterpoint = target.transform.position;
        }

        /// <summary>
        /// Checks if the input coordinates are within the sphere of attack.
        /// </summary>
        /// <param name="position">Coordinates.</param>
        /// <returns>Returns true if the coordinates are within the area, and false otherwise.</returns>
        private bool InTargetArea(Vector3 position)
        {
            return (Vector3.Distance(position, this.TargetAreaCenterpoint) < this.Radius);
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
        /// Searches for a target link within the target area. If successful, sets the property <c>Link</c> to a new <c>Attacker.TargetLink</c> object with the new <c>src_node</c> and <c>dest_node</c> nodes. Otherwise, sets the property <c>Link</c> to null. Failure may occur if no source node was found or if the source node has no links.
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
        
        // TODO: move this to the Attacker object.
        // FIXME: Packets should be sent *from* the source groundstation! Otherwise it's semantically incorrect.
        // slow down when we're close to minimum distance to improve accuracy
        public BinaryHeap<Path> FindAttackRoutes(RouteGraph rg, List<GameObject> dest_groundstations, Satellite[] satlist, ScenePainter painter, int maxsats, float maxdist, float margin, float km_per_unit, bool graph_on, GroundGrid grid) // TODO: Move this function to the attacker object.
        {
            // REVIEW: Do I want to pass the destination groundstations at the beginning?
            // NOTE: I would like to remove some of these parameters or pass them in a more elegant way.
            BinaryHeap<Path> heap = new BinaryHeap<Path>(dest_groundstations.Count * SourceGroundstations.Count); // priority queue <routegraph, routelength>
            foreach (GameObject src_gs in SourceGroundstations)
            {
                foreach (GameObject dest_gs in dest_groundstations)
                {
                    if (dest_gs == src_gs)
                    {
                        continue;
                    }

                    _RouteHandler.ResetRoute(src_gs, dest_gs, painter, satlist, maxsats);
                    rg = _RouteHandler.BuildRouteGraph(src_gs, dest_gs, maxdist, margin, maxsats, satlist, km_per_unit, graph_on, grid);
                    rg.ComputeRoutes();
                    Debug.Log($"{_Groundstations[src_gs]} to {_Groundstations[dest_gs]}");

                    Path path = FindAttackRoute(rg.startnode, rg.endnode, src_gs, dest_gs);
                    if (path != null)
                    {
                        heap.Add(path, (double)path.nodes.Count);
                    }
                }
            }
            return heap;
        }
        
        	// TODO: move this to the Attacker object.
	/* Draw the computed path and send traffic in mbits. */
	public void ExecuteAttackRoute(Path path, GameObject city1, GameObject city2, int mbits, float km_per_unit, Satellite[] satlist, LinkCapacityMonitor _link_capacities, ScenePainter _painter)
	{
        // TODO: create a attributes struct.
		Node rn = path.nodes.First();
		// REVIEW: Separate the drawing functionality from the link capacity modification.
		Debug.Assert(rn != null, "ExecuteAttackRoute | The last node is empty.");
		Debug.Assert(rn.Id == -2, "ExecuteAttackRoute | The last node is not -2. Instead, it's " + rn.Id);
		Node prevnode = null;
		Satellite sat = null;
		Satellite prevsat = null;
		int previd = -4;
		int id = -4; /* first id */
		double prevdist = km_per_unit * rn.Dist;
		int hop = 0;
		int index = 1;
		while (true)
		{

			previd = id;
			id = rn.Id;
			Debug.Log("ID: " + id);
			
			// TODO: this process of coloring links must be reversed, because the processing order of links is reversed (it goes from last node to first node)

			if (previd != -4)
			{
				// The previous and current nodes are satellites. (ISL link)
				if (previd >= 0 && id >= 0)
				{
					sat = satlist[id];
					prevsat = satlist[previd];

					// Increase the load of the link and abort if link becomes flooded.
					_link_capacities.DecreaseLinkCapacity(previd, id, mbits);
					if (_link_capacities.IsFlooded(previd, id))
					{
						break;
					}
					
					_painter.ColorRouteISLLink(prevsat, sat, prevnode, rn);
				}
				// The current node is a satellite and the previous, a city. (RF link)
				else if (id >= 0 && previd == -2)
				{
					sat = satlist[id];
					Assert.AreEqual(-2, previd);
					_painter.ColorRFLink(city2, sat, prevnode, rn);
					// TODO: make the link load capacity rule also hold for RF links. 
				}
				// The current node is a city and the previous, a satellite. (RF link)
				else 
				{
					// if (id == -1 && previd >= 0)
					sat = satlist[previd]; 
					_painter.UsedRFLinks.Add(new ActiveRF(city1, rn, sat, prevnode));
				}
			}

			if (rn != null && rn.Id == -1)
			{
				// basically; we are at the end!
				break;
			}
			prevnode = rn;
			// rn = rn.Parent;
			if (index == path.nodes.Count)
			{
				// highlight_reachable();
				return;
			}
			rn = path.nodes[index]; // TODO: reverse the node list and read everything in opposite order when processing link capacity!
			if (rn == null)
			{
				// highlight_reachable();
				return;
			}
			index++;
		}

	}
    
    // TODO: check if I can remove  this function.
    // void highlight_reachable() // CLEANUP: Remove the highlight_reachable() function.
    // {
    //     for (int satnum = 0; satnum < maxsats; satnum++)
    //     {
    //         satlist[satnum].BeamOff();
    //     };
    //     Node[] reachable = rg.GetReachableNodes();
    //     print("reachable: " + reachable.Length.ToString());
    //     for (int i = 0; i < reachable.Length; i++)
    //     {
    //         Node rn = reachable[i];
    //         if (rn.Id >= 0)
    //         {
    //             Satellite sat = satlist[rn.Id];
    //             sat.BeamOn();
    //         }
    //     }
    // }
	
        
        // TODO: Create a attacker run() function.
        /// <summary>
        /// Execute the attacker object.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Run()
        {
            throw new NotImplementedException();
        }
    }
}