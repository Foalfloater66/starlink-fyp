using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Attack.Cases;
using Orbits;
using Orbits.Satellites;
using Routing;
using Scene;
using UnityEngine;
using Utilities;

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
        private ConstellationContext _constellationContext;

        /// <summary>
        /// File for debugging.
        /// </summary>
        // private System.IO.StreamWriter logfile;
        private StreamWriter _attackLogger;

        private StreamWriter _pathLogger;

        private float km_per_unit;

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
        public Attacker(AttackerParams attackParams, float sat0r, float attack_radius, Transform transform,
            GameObject prefab, GroundstationCollection groundstations, RouteHandler route_handler, ScenePainter painter,
            LinkCapacityMonitor
                linkCapacityMonitor, StreamWriter fileWriter, StreamWriter pathWriter, float km_per_unit)
        {
            // AttackParams
            SourceGroundstations =
                attackParams.SrcGroundstations; // TODO: should this also be a groundstation collection object?
            Target = new AttackTarget(attackParams, sat0r, attack_radius, transform, prefab);

            _painter = painter;
            _linkCapacityMonitor = linkCapacityMonitor;
            _routeHandler = route_handler;

            _Groundstations = groundstations;
            _attackLogger = fileWriter;
            _pathLogger = pathWriter;
            this.km_per_unit = km_per_unit;
        }

        /// <summary>
        /// Finds the shortest route between two groundstations.
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="endNode"></param>
        /// <param name="srcGs">Source groundstation.</param>
        /// <param name="destGs">Destination groundstation.</param>
        /// <returns>If a route containing the target link is found, creates and returns the route's <c>Path</c>. Otherwise, returns null.</returns>
        public Path FindAttackRoute(Node startNode, Node endNode, GameObject srcGs, GameObject destGs)
        {
            // This function processes the route in the reverse order.
            // TODO: this EXTRACTS THE attack route.

            System.Diagnostics.Debug.Assert(Target.Link != null, "FindAttackRoute | No attacker link has been set.");

            var rn = endNode;
            var route = new Path(startNode, endNode, srcGs, destGs);
            route.Nodes.Add(rn);

            var startsatid = 0;
            var endsatid = -1;
            var id = -4;
            var prevID = -4;
            var viableRoute = false; // can the target link be attacked with this route?

            while (true)
            {
                if (rn == startNode)
                {
                    route.Nodes.Add(rn);
                    startsatid = id; // TODO: do I need to keep this?
                    break;
                }

                id = rn.Id;

                if (id >= 0) route.Nodes.Add(rn);

                if (endsatid == -1 && id >= 0) endsatid = id;

                if (id >= 0 && prevID >= 0)
                    if (prevID == Target.Link.DestNode.Id && id == Target.Link.SrcNode.Id)
                        viableRoute = true;

                prevID = id;

                rn = rn.Parent;

                if (rn == null) // this route is incomplete.
                    return null;
            }

            if (viableRoute) return route;

            return null;
        }


        /// <summary>
        /// Finds the shortest route between two groundstations.
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="endNode"></param>
        /// <param name="srcGs">Source groundstation.</param>
        /// <param name="destGs">Destination groundstation.</param>
        /// <returns>If a route containing the target link is found, creates and returns the route's <c>Path</c>. Otherwise, returns null.</returns>
        public Path ExtractRoute(Node startNode, Node endNode, GameObject srcGs, GameObject destGs)
        {
            // This function processes the route in the reverse order.
            // TODO: this EXTRACTS THE attack route.

            System.Diagnostics.Debug.Assert(Target.Link != null, "FindAttackRoute | No attacker link has been set.");

            var rn = endNode;
            var route = new Path(startNode, endNode, srcGs, destGs);
            route.Nodes.Add(rn);

            var startsatid = 0;
            var endsatid = -1;
            var id = -4;
            var prevID = -4;
            var viableRoute = false; // can the target link be attacked with this route?

            while (true)
            {
                if (rn == startNode)
                {
                    route.Nodes.Add(rn);
                    startsatid = id; // TODO: do I need to keep this?
                    break;
                }

                id = rn.Id;

                if (id >= 0) route.Nodes.Add(rn);

                if (endsatid == -1 && id >= 0) endsatid = id;

                // if (id >= 0 && prevID >= 0)
                // {
                //
                //     if (prevID == Target.Link.DestNode.Id && id == Target.Link.SrcNode.Id)
                //     {
                //         viableRoute = true;
                //     }
                // }

                prevID = id;

                rn = rn.Parent;

                if (rn == null) // this route is incomplete.
                    return null;
            }

            return route;
        }

        // TODO: I would like to put this in the defence code area. or like. some routing code.
        public Path GetRandomRoute(Node startNode, Node endNode, GameObject srcGs, GameObject destGs,
            int rmax, int mbits, bool graphOn)
        {
            // TODO: check what the randomizer uses.
            var selectedRouteId = new System.Random().Next(0, rmax);

            // Compute the multiple shortest paths and select the one for the selected random ID.
            for (var i = 0; i < rmax; i++) 
            {
                // TODO: make sure that the multipath setting work
                // TODO: the multipath setting 
                // Update the route graph in a multipath setting.
                if (i == 0)
                {
                    _routeHandler.ResetRoute(srcGs, destGs, _painter, _constellationContext.satlist,
                        _constellationContext.maxsats);
                    _routeHandler.BuildRouteGraph(srcGs, destGs, _constellationContext.maxdist,
                        _constellationContext.margin, _constellationContext.maxsats, _constellationContext.satlist,
                        _constellationContext.km_per_unit, graphOn);
                }
                else
                {
                    _rg.ResetNodeDistances();
                }

                _rg.ComputeRoutes();

                // Path route = ExtractRoute(startNode, endNode, srcGs, destGs);

                if (i == selectedRouteId) return ExtractRoute(startNode, endNode, srcGs, destGs);

                RouteHandler.LockRoute(_painter);
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
            var north_vector = new Vector3(0.0f, 1.0f, 0.0f); // Reference unit northern vector
            var target_position = src_gs.transform.position;

            var candidate_vector = dest_gs.transform.position - target_position; // Vector direction to check.
            var candidate_angle = Vector3.Angle(candidate_vector, north_vector);

            var target_vector = Target.Link.SrcNode.Position - target_position; // Desired vector direction
            var target_angle = Vector3.Angle(target_vector, north_vector);

            // NOTE: Could make these unit vectors. But it doesn't really matter.
            var difference = Mathf.Abs(target_angle - candidate_angle);

            if (difference > 90f) return true; // This candidate would not reach the target source node either way.

            return false; // This candidate might be able to reach the target source node.
        }

        private BinaryHeap<Path> _FindAttackRoutes(RouteGraph rg, List<GameObject> dest_groundstations, bool graph_on,
            ConstellationContext constellation_ctx)
        {
            // REVIEW: Do I want to pass the destination groundstations at the beginning?
            // NOTE: I would like to remove some of these parameters or pass them in a more elegant way.
            var
                heap = new BinaryHeap<Path>(dest_groundstations.Count *
                                            SourceGroundstations.Count); // priority queue <routegraph, routelength>
            foreach (var src_gs in SourceGroundstations)
            foreach (var dest_gs in dest_groundstations)
            {
                if (dest_gs == src_gs) continue;

                // Don't compute the route if the direction of the two nodes makes it unlikely that a possible
                // will exist.
                if (_IsInvalidPair(src_gs, dest_gs)) continue;
                _routeHandler.ResetRoute(src_gs, dest_gs, _painter, constellation_ctx.satlist,
                    constellation_ctx.maxsats);
                rg = _routeHandler.BuildRouteGraph(src_gs, dest_gs, constellation_ctx.maxdist,
                    constellation_ctx.margin, constellation_ctx.maxsats, constellation_ctx.satlist,
                    constellation_ctx.km_per_unit, graph_on);
                rg.ComputeRoutes();


                var path = FindAttackRoute(rg.startnode, rg.endnode, src_gs, dest_gs);


                if (path != null) heap.Add(path, (double)path.Nodes.Count);
                // return heap; // TODO: REMOVE THIS.
            }

            return heap;
        }

        public void SimulateRoute(Path path, int mbits, LinkCapacityMonitor virtualLinkMonitor)
        {
            var rn = path.Nodes.Last();

            // REVIEW: Separate the drawing functionality from the link capacity modification.
            Debug.Assert(rn != null); //, "ExecuteAttackRoute | The last node is empty.");
            Debug.Assert(rn.Id == -1,
                $"Execute AttackRoute | The last node isn't -1. Instead, got {rn.Id}"); //, "ExecuteAttackRoute | The last node is not -2. Instead, it's " + rn.Id);

            if (path.Nodes.First().Id != -2) return; // This isn't a valid path; it doesn't have a destination.
            Node prevnode = null;
            Satellite sat = null;
            Satellite prevsat = null;
            var previd = -4;
            var id = -4; /* first id */

            if (path.Nodes.Count == 1)
                // Only node -1 is present; so no real path exists.
                return;

            var index = path.Nodes.Count - 2;

            // process links from start (id: -1) to end (id: -2)
            while (true)
            {
                previd = id;
                id = rn.Id;

                if (previd != -4)
                {
                    // The previous and current nodes are satellites. (ISL link)
                    if (previd >= 0 && id >= 0)
                    {
                        // Increase the load of the link and abort if link becomes flooded.
                        if (virtualLinkMonitor.IsCongested(previd, id)) return;
                        virtualLinkMonitor.DecreaseLinkCapacity(previd, id, mbits);
                        // _painter.ColorRouteISLLink(prevsat, sat, prevnode, rn);
                    }
                    // The current node is a satellite and the previous, a city. (RF link)
                    else if (id >= 0 && previd == -1)
                    {
                        if (virtualLinkMonitor.IsCongested(_Groundstations[path.StartCity], id.ToString())) return;
                        virtualLinkMonitor.DecreaseLinkCapacity(_Groundstations[path.StartCity], id.ToString(), mbits);
                    }
                    // The current node is a city and the previous, a satellite. (RF link)
                    else if (id == -2 && previd >= 0)
                    {
                        if (virtualLinkMonitor.IsCongested(previd.ToString(), _Groundstations[path.EndCity])) return;
                        virtualLinkMonitor.DecreaseLinkCapacity(previd.ToString(), _Groundstations[path.EndCity],
                            mbits);
                        break; // We've reached the end node. Time to exit the loop.
                    }
                }

                // Update referred links

                // If we've gone through the entire path, return.
                if (index == -1) return; // only show a path if it's feasible.

                rn = path.Nodes[index];
                if (rn == null) return; // only show a path if it's feasible.

                index--;
            }
        }


        /* Draw the computed path and send traffic in mbits. */
        public void ExecuteAttackRoute(Path path, GameObject city1 /* TODO: remove this */,
            GameObject city2 /* TODO: remove this */, int mbits, LinkCapacityMonitor _linkCapacityMonitor,
            ScenePainter _painter, ConstellationContext constellation_ctx)
        {
            var rn = path.Nodes.Last();

            // REVIEW: Separate the drawing functionality from the link capacity modification.

            Debug.Assert(rn != null); //, "ExecuteAttackRoute | The last node is empty.");
            Debug.Assert(rn.Id == -1,
                $"Execute AttackRoute | The last node isn't -1. Instead, got {rn.Id}"); //, "ExecuteAttackRoute | The last node is not -2. Instead, it's " + rn.Id);

            if (path.Nodes.First().Id != -2) return; // This isn't a valid path; it doesn't have a destination.
            Node prevnode = null;
            Satellite sat = null;
            Satellite prevsat = null;
            var previd = -4;
            var id = -4; /* first id */

            if (path.Nodes.Count == 1)
                // Only node -1 is present; so no real path exists.
                return;

            var index = path.Nodes.Count - 2;

            // process links from start (id: -1) to end (id: -2)
            while (true)
            {
                previd = id;
                id = rn.Id;

                if (previd != -4)
                {
                    // The previous and current nodes are satellites. (ISL link)
                    if (previd >= 0 && id >= 0)
                    {
                        sat = constellation_ctx.satlist[id];
                        prevsat = constellation_ctx.satlist[previd];

                        // Increase the load of the link and abort if link becomes flooded.
                        // TODO: becauise im processing this backwards, I need to process the links by previd to id! not the other way around!
                        if (_linkCapacityMonitor.IsCongested(previd, id)) return;
                        _linkCapacityMonitor.DecreaseLinkCapacity(previd, id, mbits);
                        _painter.ColorRouteISLLink(prevsat, sat, prevnode, rn);
                    }
                    // The current node is a satellite and the previous, a city. (RF link)
                    else if (id >= 0 && previd == -1)
                    {
                        if (_linkCapacityMonitor.IsCongested(_Groundstations[path.StartCity], id.ToString())) return;
                        _linkCapacityMonitor.DecreaseLinkCapacity(_Groundstations[path.StartCity], id.ToString(),
                            mbits);
                        sat = constellation_ctx.satlist[id];
                        _painter.ColorRFLink(city1, sat, prevnode, rn);
                    }
                    // The current node is a city and the previous, a satellite. (RF link)
                    else if (id == -2 && previd >= 0)
                    {
                        if (_linkCapacityMonitor.IsCongested(previd.ToString(), _Groundstations[path.EndCity])) return;
                        _linkCapacityMonitor.DecreaseLinkCapacity(previd.ToString(), _Groundstations[path.EndCity],
                            mbits);
                        sat = constellation_ctx.satlist[previd];
                        _painter.ColorRFLink(city2, sat, prevnode, rn);
                        break; // We've reached the end node. Time to exit the loop.
                    }
                }

                // Update referred links
                prevnode = rn;

                // If we've gone through the entire path, return.
                if (index == -1) // only show a path if it's feasible.
                    return;

                rn = path.Nodes[index];
                if (rn == null) // only show a path if it's feasible.
                    return;

                index--;
            }
        }


        /// <summary>
        /// Create routes until the link's capacity has supposedly been reached.
        /// </summary>
        /// <param name="attack_routes">Potential attack routes.</param>
        /// <param name="constellation_ctx">Constellation context.</param>
        private List<Path> FloodLink(BinaryHeap<Path> viableAttackRoutes, int mbits)
        {
            // PICK ROUTES THAT WOULD ALLOW THE LINK'S CAPACITY TO BE SUPPOSEDLY BREACHED.
            var counter = 0;
            var sb = new StringBuilder();

            var virtualLinkCapacityMonitor = new LinkCapacityMonitor(_linkCapacityMonitor);
            var finalAttackRoutes = new List<Path>();

            // THIS FLOODING SHOULD BE PERFORMED ON THE SUPPOSED LINK CAPACITY
            while (!virtualLinkCapacityMonitor.IsCongested(Target.Link.SrcNode.Id, Target.Link.DestNode.Id) &&
                   viableAttackRoutes.Count > 0)
            {
                var route = viableAttackRoutes.ExtractMin();
                // int mbits = 4000; // sends the maximum capacity of an RF link.
                // REMEMBER THAT THE ATTACKER CANNOT MONITOR THE CAPACITY OF AN RF LINK!
                if (!RouteHandler.RouteHasEarlyCollisions(route, mbits, Target.Link.SrcNode, Target.Link.DestNode,
                        virtualLinkCapacityMonitor, _Groundstations[route.StartCity],
                        _Groundstations[route.EndCity]))
                {
                    // TODO: the attacker is going to SIMULATE this. and stop when they think it's full. and then the actual attack routes will be executed.
                    // TODO: remove the placeholder value for the maximum count.
                    // If the defender is enabled, the attack routes are replaced by the mitigation algorithm.
                    // if (defenceOn)
                    // {
                    //     GetRandomAttackRoute(attack_path.StartNode, attack_path.EndNode, attack_path.StartCity, attack_path.EndCity, 5, mbits, graphOn);
                    // }

                    // break;
                    // else
                    // {

                    // TODO: do not EXECUTE the attack route. SIMULATE it.

                    SimulateRoute(route, mbits, virtualLinkCapacityMonitor);
                    finalAttackRoutes.Add(route);
                    // ExecuteAttackRoute(attack_path, attack_path.StartCity, attack_path.EndCity, mbits,
                    // virtualLinkCapacityMonitor, _painter, constellation_ctx);

                    // }

                    // attack_path = GetRandomAttackRoute(attack_path.StartNode, attack_path.EndNode, attack_path.StartCity, attack_path.EndCity, 5, mbits);
                    // pick a random value between a set number of possible values.

                    sb.Append($",{_Groundstations[route.StartCity]} -> {_Groundstations[route.EndCity]}");
                    counter += 1;
                }
            }

            _attackLogger.Write($",{counter}");
            _pathLogger.Write(sb.ToString());

            return finalAttackRoutes;
        }

        /// <summary>
        /// Update the Target Link Visuals
        /// </summary>
        private void UpdateTargetLinkVisuals(ConstellationContext constellation_ctx)
        {
            // Debugging messages.
            // Color the target link on the map. If flooded, the link is colored red. Otherwise, the link is colored pink.
            // Checks if the link has any capacity left
            if (_linkCapacityMonitor.IsCongested(Target.Link.SrcNode.Id, Target.Link.DestNode.Id))
                _painter.ColorTargetISLLink(constellation_ctx.satlist[Target.Link.SrcNode.Id],
                    constellation_ctx.satlist[Target.Link.DestNode.Id], Target.Link.DestNode, Target.Link.SrcNode,
                    true);
            else
                _painter.ColorTargetISLLink(constellation_ctx.satlist[Target.Link.SrcNode.Id],
                    constellation_ctx.satlist[Target.Link.DestNode.Id], Target.Link.DestNode, Target.Link.SrcNode,
                    false);
        }

        /// <summary>
        /// Compute RTT of a route in milliseconds.
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        private float GetRTT(Path route)
        {
            float endDist = km_per_unit * route.EndNode.Dist;
            return 2 * endDist / 299.792f; // NOTE: where does this 299.792f come from?
        }
        
        /// <summary>
        /// Execute the attacker object.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public List<float> Run(ConstellationContext constellation_ctx, bool graph_on, List<GameObject> groundstations,
            bool defenceOn)
        {
            _constellationContext = constellation_ctx;
            _routeHandler.ResetRoute(_Groundstations["New York"], _Groundstations["Toronto"], _painter,
                constellation_ctx.satlist, constellation_ctx.maxsats);
            _rg = _routeHandler.BuildRouteGraph(_Groundstations["New York"], _Groundstations["Toronto"],
                constellation_ctx.maxdist, constellation_ctx.margin, constellation_ctx.maxsats,
                constellation_ctx.satlist, constellation_ctx.km_per_unit, graph_on);

            List<float> rttList = new List<float>();
            // If the current link isn't valid, select a new target link.
            if (!Target.HasValidTargetLink())
                // should make the debug on false!
                Target.ChangeTargetLink(_rg, true);

            // If the attacker has selected a valid link, attempt to attack it
            if (Target.Link != null && Target.HasValidTargetLink())
            {
                _attackLogger.Write($",{Target.Link.SrcNode.Id.ToString()} -> {Target.Link.DestNode.Id.ToString()}");

                // Find viable attack routes.
                var viableAttackRoutes = _FindAttackRoutes(_rg,
                    groundstations,
                    graph_on,
                    constellation_ctx
                );

                // Send the maximum capacity of an RF link.
                var mbits = 4000;

                // Extract the attack routes that are needed to congest the victim link.
                var finalAttackRoutes = FloodLink(viableAttackRoutes, mbits);

                var counter = 0;

                // Execute the final list of attack routes.
                foreach (var route in finalAttackRoutes)
                {
                    var executedRoute = route;

                    if (defenceOn)
                        executedRoute = GetRandomRoute(route.StartNode, route.EndNode, route.StartCity,
                            route.EndCity, 3, mbits, graph_on);

                    // executedRoute.
                    ExecuteAttackRoute(executedRoute, route.StartCity, route.EndCity, mbits, _linkCapacityMonitor,
                        _painter, _constellationContext);

                    rttList.Add(GetRTT(executedRoute));


                    if (counter == 3) break; // wtf??? what is this??

                    counter++;
                }

                UpdateTargetLinkVisuals(constellation_ctx);
                _attackLogger.Write(
                    $",{_linkCapacityMonitor.GetCapacity(Target.Link.SrcNode.Id, Target.Link.DestNode.Id)}");
            }
            else
            {
                _attackLogger.Write(",,0,nan");
                _pathLogger.Write(",nan");
            }

            return rttList;
        }
    }
}