using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Attack.Cases;
using Orbits;
using Routing;
using Scene;
using UnityEngine;
using Utilities;
using Path = Routing.Path;

namespace Attack
{
    /**
     * @author Morgane Marie Ohlig
    */
    /// Class <c>Attacker</c> provides a set of properties and methods used to target and flood links in a geographic area of specified center and radius.
    public class Attacker
    {
        /// List of source groundstations that the attacker can send packets from.
        private List<GameObject> SourceGroundstations { get; set; }

        // TODO: I need to check what I actually need to keep here.
        private RouteHandler _routeHandler;
        private ScenePainter _painter;
        private LinkCapacityMonitor _linkCapacityMonitor;
        private GroundstationCollection _Groundstations;
        private RouteGraph _rg;
        public AttackTarget Target;
        private Constellation _constellation;

        /// <summary>
        /// File for debugging.
        /// </summary>
        // private System.IO.StreamWriter logfile;
        private StreamWriter _attackLogger;

        private StreamWriter _pathLogger;

        /// Constructor creating an <c>Attacker</c> object with an attack area around the provided <c>(latitude, longitude)</c> coordinates. 
        public Attacker(AttackerParams attackParams, float sat0r, float attack_radius, Transform transform,
            GameObject prefab, GroundstationCollection groundstations, RouteHandler route_handler, ScenePainter painter,
            LinkCapacityMonitor
                linkCapacityMonitor, Constellation constellation, StreamWriter fileWriter, StreamWriter pathWriter)
        {
            // AttackParams
            SourceGroundstations =
                attackParams.SrcGroundstations; // TODO: should this also be a groundstation collection object?
            Target = new AttackTarget(attackParams, sat0r, attack_radius, transform, prefab);

            _painter = painter;
            _linkCapacityMonitor = linkCapacityMonitor;
            _routeHandler = route_handler;
            _constellation = constellation;
            _Groundstations = groundstations;
            _attackLogger = fileWriter;
            _pathLogger = pathWriter;
        }

        /// Finds the shortest route between two groundstations.
        public Path FindAttackRoute(Node startNode, Node endNode, GameObject srcGs, GameObject destGs)
        {
            // This function processes the route in the reverse order.
            // TODO: this EXTRACTS THE attack route.

            System.Diagnostics.Debug.Assert(Target.Link != null, "FindAttackRoute | No attacker link has been set.");

            var rn = endNode;
            var route = new Path(startNode, endNode, srcGs, destGs, 4000);
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


        /// Removes any path pairs that we know would already be invalid.
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

        private BinaryHeap<Path> _FindAttackRoutes(RouteGraph rg, List<GameObject> dest_groundstations)
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
                _routeHandler.ResetRoute(src_gs, dest_gs, _painter, _constellation.satlist,
                    _constellation.maxsats);
                rg = _routeHandler.BuildRouteGraph(src_gs, dest_gs, _constellation.maxdist,
                    _constellation.margin, _constellation.maxsats, _constellation.satlist,
                    _constellation.km_per_unit);
                rg.ComputeRoutes();


                var path = FindAttackRoute(rg.startnode, rg.endnode, src_gs, dest_gs);


                if (path != null) heap.Add(path, (double)path.Nodes.Count);
                // return heap; // TODO: REMOVE THIS.
            }

            return heap;
        }

        private void SimulateRoute(Path path, int mbits, LinkCapacityMonitor virtualLinkMonitor)
        {
            var rn = path.Nodes.Last();

            // REVIEW: Separate the drawing functionality from the link capacity modification.
            Debug.Assert(rn != null); //, "ExecuteAttackRoute | The last node is empty.");
            Debug.Assert(rn.Id == -1,
                $"Execute AttackRoute | The last node isn't -1. Instead, got {rn.Id}"); //, "ExecuteAttackRoute | The last node is not -2. Instead, it's " + rn.Id);

            if (path.Nodes.First().Id != -2) return; // This isn't a valid path; it doesn't have a destination.
            // Node prevnode = null;
            // Satellite sat = null;
            // Satellite prevsat = null;
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


        /// Create routes until the link's capacity has supposedly been reached.
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


        /// Execute the attacker object.
        public List<Path> Run(List<GameObject> groundstations)
        {
            // _constellation = constellation_ctx;
            _routeHandler.ResetRoute(_Groundstations["New York"], _Groundstations["Toronto"], _painter,
                _constellation.satlist, _constellation.maxsats);
            _rg = _routeHandler.BuildRouteGraph(_Groundstations["New York"], _Groundstations["Toronto"],
                _constellation.maxdist, _constellation.margin, _constellation.maxsats,
                _constellation.satlist, _constellation.km_per_unit);

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
                    groundstations);

                // Send the maximum capacity of an RF link.
                var mbits = 4000;

                // Extract the attack routes that are needed to congest the victim link.
                return FloodLink(viableAttackRoutes, mbits);
                // TODO: just return this.
            }

            _attackLogger.Write(",,0,nan");
            _pathLogger.Write(",nan");
            return new List<Path>();
        }
    }
}