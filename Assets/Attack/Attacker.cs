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
        private List<GameObject> SourceGroundstations { get; }
        private readonly RouteHandler _routeHandler;
        private readonly ScenePainter _painter;
        private readonly LinkCapacityMonitor _linkCapacityMonitor;
        private readonly GroundstationCollection _groundstations;
        private RouteGraph _rg;
        public readonly AttackTarget Target;
        private readonly Constellation _constellation;

        /// <summary>
        /// File for debugging.
        /// </summary>
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
            _groundstations = groundstations;
            _attackLogger = fileWriter;
            _pathLogger = pathWriter;
        }

        /// Finds the shortest route between two groundstations.
        private Path FindAttackRoute(Node startNode, Node endNode, GameObject srcGs, GameObject destGs)
        {
            System.Diagnostics.Debug.Assert(Target.Link != null, "FindAttackRoute | No attacker link has been set.");
            
            // TODO: extract the route VS. check if route is eligible.

            var rn = endNode;
            var route = new Path(startNode, endNode, srcGs, destGs, 4000);
            route.Nodes.Add(rn);

            var endsatid = -1;
            var id = -4;
            var prevID = -4;
            var viableRoute = false; 

            while (true)
            {
                if (rn == startNode)
                {
                    route.Nodes.Add(rn);
                    break;
                }
                id = rn.Id;
                if (id >= 0) route.Nodes.Add(rn);
                if (endsatid == -1 && id >= 0) endsatid = id;
                if (id >= 0 && prevID >= 0)
                    if (prevID == Target.Link.DestNode.Id && id == Target.Link.SrcNode.Id)
                        viableRoute = true; // check if this route can be used to attack the target link.
                prevID = id;
                rn = rn.Parent;
                if (rn == null) // this route is incomplete.
                    return null;
            }

            if (viableRoute) return route;

            return null;
        }


        /// Removes any path pairs that we know would already be invalid.
        private bool _IsInvalidPair(GameObject srcGs, GameObject destGs)
        {
            var northVector = new Vector3(0.0f, 1.0f, 0.0f); // Reference unit northern vector
            var targetPosition = srcGs.transform.position;
            var candidateVector = destGs.transform.position - targetPosition; // Vector direction to check.
            var candidateAngle = Vector3.Angle(candidateVector, northVector);
            var targetVector = Target.Link.SrcNode.Position - targetPosition; // Desired vector direction
            var targetAngle = Vector3.Angle(targetVector, northVector);
            var difference = Mathf.Abs(targetAngle - candidateAngle);
            if (difference > 90f) return true; // This candidate would not reach the target source node either way.
            return false; // This candidate might be able to reach the target source node.
        }

        private BinaryHeap<Path> _FindAttackRoutes(RouteGraph rg, List<GameObject> dest_groundstations)
        {
            var heap = new BinaryHeap<Path>(
                dest_groundstations.Count * SourceGroundstations.Count); // priority queue <routegraph, routelength>
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
            }

            return heap;
        }

        private void SimulateRoute(Path path, int mbits, LinkCapacityMonitor virtualLinkMonitor)
        {
            var rn = path.Nodes.Last();

            Debug.Assert(rn != null, "ExecuteAttackRoute | The last node is empty.");
            Debug.Assert(rn.Id == -1,
                $"Execute AttackRoute | The last node isn't -1. Instead, got {rn.Id}");

            if (path.Nodes.First().Id != -2) return; // This isn't a valid path; it doesn't have a destination.
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
                    }
                    // The current node is a satellite and the previous, a city. (RF link)
                    else if (id >= 0 && previd == -1)
                    {
                        if (virtualLinkMonitor.IsCongested(_groundstations[path.StartCity], id.ToString())) return;
                        virtualLinkMonitor.DecreaseLinkCapacity(_groundstations[path.StartCity], id.ToString(), mbits);
                    }
                    // The current node is a city and the previous, a satellite. (RF link)
                    else if (id == -2 && previd >= 0)
                    {
                        if (virtualLinkMonitor.IsCongested(previd.ToString(), _groundstations[path.EndCity])) return;
                        virtualLinkMonitor.DecreaseLinkCapacity(previd.ToString(), _groundstations[path.EndCity],
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
            var counter = 0;
            var sb = new StringBuilder();
            var virtualLinkCapacityMonitor = new LinkCapacityMonitor(_linkCapacityMonitor);
            var finalAttackRoutes = new List<Path>();

            while (!virtualLinkCapacityMonitor.IsCongested(Target.Link.SrcNode.Id, Target.Link.DestNode.Id) &&
                   viableAttackRoutes.Count > 0)
            {
                var route = viableAttackRoutes.ExtractMin();
                if (!RouteHandler.RouteHasEarlyCollisions(route, mbits, Target.Link.SrcNode, Target.Link.DestNode,
                        virtualLinkCapacityMonitor, _groundstations[route.StartCity],
                        _groundstations[route.EndCity]))
                {
                    SimulateRoute(route, mbits, virtualLinkCapacityMonitor);
                    finalAttackRoutes.Add(route);
                    sb.Append($",{_groundstations[route.StartCity]} -> {_groundstations[route.EndCity]}"); // TODO: I can move this to the end.
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
            _routeHandler.ResetRoute(_groundstations["New York"], _groundstations["Toronto"], _painter,
                _constellation.satlist, _constellation.maxsats);
            _rg = _routeHandler.BuildRouteGraph(_groundstations["New York"], _groundstations["Toronto"],
                _constellation.maxdist, _constellation.margin, _constellation.maxsats,
                _constellation.satlist, _constellation.km_per_unit);

            // If the current link isn't valid, select a new target link.
            if (!Target.HasValidTargetLink())
                Target.ChangeTargetLink(_rg);

            // If the attacker has selected a valid link, attempt to attack it
            if (Target.Link != null && Target.HasValidTargetLink())
            {
                _attackLogger.Write($",{Target.Link.SrcNode.Id.ToString()} -> {Target.Link.DestNode.Id.ToString()}");
                var viableAttackRoutes = _FindAttackRoutes(_rg, groundstations);
                return FloodLink(viableAttackRoutes, 4000);     // send the maximum RF link capacity.
            }

            _attackLogger.Write(",,0,nan");
            _pathLogger.Write(",nan");
            return new List<Path>();
        }
    }
}