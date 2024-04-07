using System;
using System.Collections.Generic;
using System.Linq;
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
        /// List of source groundstations that the attacker can send packets from.
        /// </summary>
        private List<GameObject> SourceGroundstations { get; set; }
        private RouteHandler _routeHandler;
        private ScenePainter _painter;
        private LinkCapacityMonitor _linkCapacityMonitor;
        private GroundstationCollection _Groundstations;
        private RouteGraph _rg;
        private AttackTarget Target;

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
            List<GameObject> src_groundstations, Transform transform, GameObject prefab, GroundstationCollection groundstations, RouteHandler route_handler, ScenePainter painter, LinkCapacityMonitor
                linkCapacityMonitor)
        { 
            SourceGroundstations = src_groundstations; // TODO: should this also be a groundstation collection object?
            Target = new AttackTarget(latitude, longitude, sat0r, attack_radius, transform, prefab);
            
            _painter = painter;
            _linkCapacityMonitor = linkCapacityMonitor;
            _routeHandler = route_handler;
            
            _Groundstations = groundstations;
            
            logfile = new System.IO.StreamWriter(Main.log_directory + "/Path/attacker_summary.txt");
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
            System.Diagnostics.Debug.Assert(Target.Link != null, "FindAttackRoute | No attacker link has been set.");

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
                    if (prev_id == Target.Link.SrcNode.Id && id == Target.Link.DestNode.Id)
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
        
        // FIXME: Packets should be sent *from* the source groundstation! Otherwise it's semantically incorrect.
        // slow down when we're close to minimum distance to improve accuracy
        private BinaryHeap<Path> _FindAttackRoutes(RouteGraph rg, List<GameObject> dest_groundstations, bool graph_on, ConstellationContext constellation_ctx)
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

                    _routeHandler.ResetRoute(src_gs, dest_gs, _painter, constellation_ctx.satlist, constellation_ctx.maxsats);
                    rg = _routeHandler.BuildRouteGraph(src_gs, dest_gs, constellation_ctx.maxdist, constellation_ctx.margin, constellation_ctx.maxsats, constellation_ctx.satlist, constellation_ctx.km_per_unit, graph_on);
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
        
		/* Draw the computed path and send traffic in mbits. */
		public void ExecuteAttackRoute(Path path, GameObject city1, GameObject city2, int mbits, LinkCapacityMonitor _linkCapacityMonitor, ScenePainter _painter, ConstellationContext constellation_ctx)
		{
	        // TODO: create a attributes struct.
			Node rn = path.nodes.First();
			// REVIEW: Separate the drawing functionality from the link capacity modification.
			
			Debug.Assert(rn != null); //, "ExecuteAttackRoute | The last node is empty.");
			Debug.Assert(rn.Id == -2); //, "ExecuteAttackRoute | The last node is not -2. Instead, it's " + rn.Id);
			
			Node prevnode = null;
			Satellite sat = null;
			Satellite prevsat = null;
			int previd = -4;
			int id = -4; /* first id */
			//double prevdist = km_per_unit * rn.Dist;
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
						sat = constellation_ctx.satlist[id];
						prevsat = constellation_ctx.satlist[previd];

						// Increase the load of the link and abort if link becomes flooded.
						_linkCapacityMonitor.DecreaseLinkCapacity(previd, id, mbits);
						if (_linkCapacityMonitor.IsFlooded(previd, id))
						{
							break;
						}
						
						_painter.ColorRouteISLLink(prevsat, sat, prevnode, rn);
					}
					// The current node is a satellite and the previous, a city. (RF link)
					else if (id >= 0 && previd == -2)
					{
						sat = constellation_ctx.satlist[id];
						Assert.AreEqual(-2, previd);
						_painter.ColorRFLink(city2, sat, prevnode, rn);
						// TODO: make the link load capacity rule also hold for RF links. 
					}
					// The current node is a city and the previous, a satellite. (RF link)
					else 
					{
						// if (id == -1 && previd >= 0)
						sat = constellation_ctx.satlist[previd]; 
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
				if (index == path.nodes.Count) // only show a path if it's feasible.
				{
					return;
				}
				rn = path.nodes[index]; // TODO: reverse the node list and read everything in opposite order when processing link capacity!
				if (rn == null) // only show a path if it's feasible.
				{
					return;
				}
				index++;
			}

		}

		/// <summary>
		/// Create routes until the link's capacity has been reached.
		/// </summary>
		/// <param name="attack_routes">Potential attack routes.</param>
		/// <param name="constellation_ctx">Constellation context.</param>
		private void FloodLink(BinaryHeap<Path> attack_routes, ConstellationContext constellation_ctx)
		{
			while (!_linkCapacityMonitor.IsFlooded(Target.Link.SrcNode.Id, Target.Link.DestNode.Id) && attack_routes.Count > 0)
			{
				Path attack_path = attack_routes.ExtractMin();
				int mbits = 600;
				if (!RouteHandler.RouteHasEarlyCollisions(attack_path, mbits, Target.Link.SrcNode, Target.Link.DestNode,
					    _linkCapacityMonitor))
				{
					ExecuteAttackRoute(attack_path, attack_path.start_city, attack_path.end_city, mbits,
						_linkCapacityMonitor, _painter, constellation_ctx);
				}
			}
		}

		/// <summary>
		/// Update the Target Link Visuals
		/// </summary>
		private void UpdateTargetLinkVisuals(ConstellationContext constellation_ctx)
		{
			// Debugging messages.
			// Color the target link on the map. If flooded, the link is colored red. Otherwise, the link is colored pink.
			if (_linkCapacityMonitor.IsFlooded(Target.Link.SrcNode.Id, Target.Link.DestNode.Id))
			{
				Debug.Log("Update | Link was flooded.");
				_painter.ColorTargetISLLink(constellation_ctx.satlist[Target.Link.SrcNode.Id], constellation_ctx.satlist[Target.Link.DestNode.Id], Target.Link.DestNode, Target.Link.SrcNode, true);
			}
			else
			{
				Debug.Log("Update | Link could not be flooded. It has capacity: " +
				          _linkCapacityMonitor.GetCapacity(Target.Link.SrcNode.Id, Target.Link.DestNode.Id));
				_painter.ColorTargetISLLink(constellation_ctx.satlist[Target.Link.SrcNode.Id], constellation_ctx.satlist[Target.Link.DestNode.Id], Target.Link.DestNode, Target.Link.SrcNode, false);
			}
		}
        
        /// <summary>
        /// Execute the attacker object.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Run(ConstellationContext constellation_ctx,
	        bool graph_on, List<GameObject> groundstations)
        {
	        // TODO: Create a RouteGraph without needing to have new_york and toronto passed.
	        _routeHandler.ResetRoute(_Groundstations["New York"], _Groundstations["Toronto"], _painter, constellation_ctx.satlist,constellation_ctx.maxsats);
	        // TODO: Do I need to return the routegraph?
	        _rg = _routeHandler.BuildRouteGraph(_Groundstations["New York"], _Groundstations["Toronto"], constellation_ctx.maxdist, constellation_ctx.margin, constellation_ctx.maxsats, constellation_ctx.satlist, constellation_ctx.km_per_unit, graph_on);


	        // If the current link isn't valid, select a new target link.
	        if (!Target.HasValidTargetLink())
	        {
		        Target.ChangeTargetLink(_rg, debug_on: true);
	        }

	        // If the attacker has selected a valid link, attempt to attack it
	        if (Target.Link != null && Target.HasValidTargetLink())
	        {
				Debug.Log($"Attacker.Run | Link: {Target.Link.SrcNode.Id} - {Target.Link.DestNode.Id}");
		        // Find viable attack routes.
		        
		        BinaryHeap<Path> attack_routes = _FindAttackRoutes(_rg,
			        groundstations,
			        graph_on,
			        constellation_ctx
			        );

		        FloodLink(attack_routes, constellation_ctx);
		        UpdateTargetLinkVisuals(constellation_ctx);
	        }
	        else
	        {
		        Debug.Log("Attacker.Update | Could not find any valid link.");
	        }
        }
    }
}