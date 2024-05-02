using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Orbits;
using Orbits.Satellites;
using Routing;
using Scene;
using UnityEditor;
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
        public AttackTarget Target;

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
        public Attacker(AttackParams attackParams, float sat0r, float attack_radius, Transform transform,
            GameObject prefab, GroundstationCollection groundstations, RouteHandler route_handler, ScenePainter painter,
            LinkCapacityMonitor
                linkCapacityMonitor)
        {
            // AttackParams
            SourceGroundstations =
                attackParams.SrcGroundstations; // TODO: should this also be a groundstation collection object?
            Target = new AttackTarget(attackParams, sat0r, attack_radius, transform, prefab);

            _painter = painter;
            _linkCapacityMonitor = linkCapacityMonitor;
            _routeHandler = route_handler;

            _Groundstations = groundstations;

            // logfile = new System.IO.StreamWriter(Main.log_directory + "/Path/attacker_summary.txt");
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
            // This function processes the route in the reverse order.
            
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
                    // if (prev_id == Target.Link.SrcNode.Id && id == Target.Link.DestNode.Id)
                    // {
                    //     viable_route = true;
                    // }

                    if (prev_id == Target.Link.DestNode.Id && id == Target.Link.SrcNode.Id)
                    {
                        viable_route = true; // REMOVE THIS!
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
            // viable_route = true;
            if (viable_route)
            {
                string name1 = _Groundstations[src_gs];
                string name2 = _Groundstations[dest_gs];
                // Debug.Log($"IN: {name1} OUT: {name2}");
                StringBuilder builder = new StringBuilder();
                builder.Append($"IN: {name1} of id {start_node.Id} OUT: {name2} of id {end_node.Id} ");
                foreach (Node node in route.nodes)
                {
                    builder.Append($"- {node.Id} ");
                }

                // if (viable_route)
                // {
                    builder.Append($" = VALID NODE");
                // }

                Debug.Log(builder.ToString());
                return route;
            }

            return null;
        }

        /// <summary>
        /// Removes any path pairs that we know would already be invalid.
        /// TODO: rename this.
        /// </summary>
        /// <returns></returns>
        private bool _IsInvalidPair(GameObject src_gs, GameObject dest_gs)
        {
            Vector3 north_vector = new Vector3(0.0f, 1.0f, 0.0f); // Reference unit northern vector
            Vector3 target_position = src_gs.transform.position;

            Vector3 candidate_vector = dest_gs.transform.position - target_position; // Vector direction to check.
            float candidate_angle = Vector3.Angle(candidate_vector, north_vector);

            Vector3 target_vector = Target.Link.SrcNode.Position - target_position; // Desired vector direction
            float target_angle = Vector3.Angle(target_vector, north_vector);

            // NOTE: Could make these unit vectors. But it doesn't really matter.
            float difference = Mathf.Abs(target_angle - candidate_angle);

            if (difference > 90f)
            {
                return true; // This candidate would not reach the target source node either way.
            }

            return false; // This candidate might be able to reach the target source node.
        }

        // FIXME: Packets should be sent *from* the source groundstation! Otherwise it's semantically incorrect.
        // slow down when we're close to minimum distance to improve accuracy
        private BinaryHeap<Path> _FindAttackRoutes(RouteGraph rg, List<GameObject> dest_groundstations, bool graph_on,
            ConstellationContext constellation_ctx)
        {
            // REVIEW: Do I want to pass the destination groundstations at the beginning?
            // NOTE: I would like to remove some of these parameters or pass them in a more elegant way.
            BinaryHeap<Path>
                heap = new BinaryHeap<Path>(dest_groundstations.Count *
                                            SourceGroundstations.Count); // priority queue <routegraph, routelength>
            foreach (GameObject src_gs in SourceGroundstations)
            {
                foreach (GameObject dest_gs in dest_groundstations)
                {
                    if (dest_gs == src_gs)
                    {
                        continue;
                    }

                    if (_IsInvalidPair(src_gs, dest_gs))
                    {
                        continue;
                    }

                    // TODO: check here. before rebuilding the entire routegraph.


                    _routeHandler.ResetRoute(src_gs, dest_gs, _painter, constellation_ctx.satlist,
                        constellation_ctx.maxsats);
                    rg = _routeHandler.BuildRouteGraph(src_gs, dest_gs, constellation_ctx.maxdist,
                        constellation_ctx.margin, constellation_ctx.maxsats, constellation_ctx.satlist,
                        constellation_ctx.km_per_unit, graph_on);
                    rg.ComputeRoutes();


                    Path path = FindAttackRoute(rg.startnode, rg.endnode, src_gs, dest_gs);


                    if (path != null)
                    {
                        heap.Add(path, (double)path.nodes.Count);
                        // return heap; // TODO: REMOVE THIS.
                    }
                }
            }

            return heap;
        }

        /* Draw the computed path and send traffic in mbits. */
        public void ExecuteAttackRoute(Path path, GameObject city1 /* TODO: remove this */,
            GameObject city2 /* TODO: remove this */, int mbits, LinkCapacityMonitor _linkCapacityMonitor,
            ScenePainter _painter, ConstellationContext constellation_ctx)
        {
            Node rn = path.nodes.Last();

            // REVIEW: Separate the drawing functionality from the link capacity modification.

            Debug.Assert(rn != null); //, "ExecuteAttackRoute | The last node is empty.");
            Debug.Assert(rn.Id == -1,
                $"Execute AttackRoute | The last node isn't -1. Instead, got {rn.Id}"); //, "ExecuteAttackRoute | The last node is not -2. Instead, it's " + rn.Id);

            if (path.nodes.First().Id != -2)
            {
                 
                return;         // This isn't a valid path; it doesn't have a destination.
            }
            Node prevnode = null;
            Satellite sat = null;
            Satellite prevsat = null;
            int previd = -4;
            int id = -4; /* first id */

            if (path.nodes.Count == 1)
            {
                // Only node -1 is present; so no real path exists.
                return;
            }

            int index = path.nodes.Count - 2;

            // process links from start (id: -2) to end (id: -1)
            // process links from start (id: -1) to end (id: -2)
            while (true)
            {
                previd = id;
                id = rn.Id;
                // Debug.Log($"ID: {id}");

                if (previd != -4)
                {
                    // The previous and current nodes are satellites. (ISL link)
                    if (previd >= 0 && id >= 0)
                    {
                        sat = constellation_ctx.satlist[id];
                        prevsat = constellation_ctx.satlist[previd];

                        // Increase the load of the link and abort if link becomes flooded.
                        // TODO: becauise im processing this backwards, I need to process the links by previd to id! not the other way around!
                        _linkCapacityMonitor.DecreaseLinkCapacity(previd, id, mbits);
                        if (_linkCapacityMonitor.IsFlooded(previd, id))
                        {
                            return;
                        }

                        _painter.ColorRouteISLLink(prevsat, sat, prevnode, rn);
                    }
                    // The current node is a satellite and the previous, a city. (RF link)
                    else if (id >= 0 && previd == -1)
                    {
                        _linkCapacityMonitor.DecreaseLinkCapacity(_Groundstations[path.end_city], id.ToString(), mbits);
                        if (_linkCapacityMonitor.IsFlooded(_Groundstations[path.end_city], id.ToString()))
                        {
                            return;
                        }

                        sat = constellation_ctx.satlist[id];
                        _painter.ColorRFLink(city1, sat, prevnode, rn);
                        // TODO: make the link load capacity rule also hold for RF links. 
                    }
                    // The current node is a city and the previous, a satellite. (RF link)
                    else if (id == -2 && previd >= 0)
                    {
                        _linkCapacityMonitor.DecreaseLinkCapacity(previd.ToString(), _Groundstations[path.start_city],
                            mbits);
                        if (_linkCapacityMonitor.IsFlooded(previd.ToString(), _Groundstations[path.start_city]))
                        {
                            return;
                        }

                        sat = constellation_ctx.satlist[previd];
                        _painter.ColorRFLink(city2, sat, prevnode, rn);
                        break;                                                  // We've reached the end node. Time to exit the loop.
                        // _painter.UsedRFLinks.Add(new ActiveRF(city2, rn, sat, prevnode));
                    }
                }

                // Update referred links

                // We've reached the end node. Time to exit the loop!
                // if (rn.Id == -2)
                // {
                //     break;
                // }

                prevnode = rn;

                // If we've gone through the entire path, return.
                if (index == -1) // only show a path if it's feasible.
                {
                    return;
                }

                rn = path.nodes[index]; 
                // TODO: reverse the node list and read everything in opposite order when processing link capacity!
                if (rn == null) // only show a path if it's feasible.
                {
                    return;
                }

                index--;
            }
        }

        /// <summary>
        /// Create routes until the link's capacity has been reached.
        /// </summary>
        /// <param name="attack_routes">Potential attack routes.</param>
        /// <param name="constellation_ctx">Constellation context.</param>
        private void FloodLink(BinaryHeap<Path> attack_routes, ConstellationContext constellation_ctx)
        {
            int counter = 0;
            while (!_linkCapacityMonitor.IsFlooded(Target.Link.SrcNode.Id, Target.Link.DestNode.Id) &&
                   attack_routes.Count > 0)
            {
                Path attack_path = attack_routes.ExtractMin();
                int mbits = 600; // TODO: make this a variable.
                if (!RouteHandler.RouteHasEarlyCollisions(attack_path, mbits, Target.Link.SrcNode, Target.Link.DestNode,
                        _linkCapacityMonitor, _Groundstations[attack_path.start_city],
                        _Groundstations[attack_path.end_city]))
                {
                    ExecuteAttackRoute(attack_path, attack_path.start_city, attack_path.end_city, mbits,
                        _linkCapacityMonitor, _painter, constellation_ctx);
                    counter += 1;
                }
            }

            Debug.Log($"I have created {counter} attack routes.");
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
                Debug.Log("UpdateTargetLinkVisuals | Link was flooded.");
                _painter.ColorTargetISLLink(constellation_ctx.satlist[Target.Link.SrcNode.Id],
                    constellation_ctx.satlist[Target.Link.DestNode.Id], Target.Link.DestNode, Target.Link.SrcNode,
                    true);
            }
            else
            {
                Debug.Log("UpdateTargetLinkVisuals | Link could not be flooded. It has capacity: " +
                          _linkCapacityMonitor.GetCapacity(Target.Link.SrcNode.Id, Target.Link.DestNode.Id));
                _painter.ColorTargetISLLink(constellation_ctx.satlist[Target.Link.SrcNode.Id],
                    constellation_ctx.satlist[Target.Link.DestNode.Id], Target.Link.DestNode, Target.Link.SrcNode,
                    false);
            }
        }

        /// <summary>
        /// Execute the attacker object.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Run(ConstellationContext constellation_ctx,
            bool graph_on, List<GameObject> groundstations)
        {
            _routeHandler.ResetRoute(_Groundstations["New York"], _Groundstations["Toronto"], _painter,
                constellation_ctx.satlist, constellation_ctx.maxsats);
            _rg = _routeHandler.BuildRouteGraph(_Groundstations["New York"], _Groundstations["Toronto"],
                constellation_ctx.maxdist, constellation_ctx.margin, constellation_ctx.maxsats,
                constellation_ctx.satlist, constellation_ctx.km_per_unit, graph_on);

            // If the current link isn't valid, select a new target link.
            if (!Target.HasValidTargetLink())
            {
                // should make the debug on false!
                Target.ChangeTargetLink(_rg, debug_on: true);
            }

            // If the attacker has selected a valid link, attempt to attack it
            if (Target.Link != null && Target.HasValidTargetLink())
            {
                Debug.Log($"Attacker.Run | Target Link: {Target.Link.SrcNode.Id} - {Target.Link.DestNode.Id}");

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
                Debug.Log("Attacker.Run | Could not find any valid link.");
            }
        }
    }
}