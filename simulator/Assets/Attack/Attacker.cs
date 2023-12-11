/* C138 Final Year Project 2023-2024 */

using System.Collections.Generic;
using System.Linq;
using Attack.Cases;
using Orbits;
using Routing;
using Scene;
using UnityEngine;
using Utilities;

namespace Attack
{
    /// Class <c>Attacker</c> provides a set of properties and methods used to target and flood links in a geographic area of specified center and radius.
    public class Attacker
    {
        private List<GameObject> SourceGroundstations { get; }

        // private readonly RouteHandler _routeHandler;
        private readonly ScenePainter _painter;
        private readonly LinkCapacityMonitor _linkCapacityMonitor;
        private readonly GroundstationCollection _groundstations;
        private RouteGraph _rg;
        public readonly AttackTarget Target;
        private readonly Constellation _constellation;

        /// Constructor creating an <c>Attacker</c> object with an attack area around the provided <c>(latitude, longitude)</c> coordinates. 
        public Attacker(AttackerParams attackParams, float sat0r, float attack_radius, Transform transform,
            GameObject prefab, GroundstationCollection groundstations, RouteGraph rg, ScenePainter painter,
            LinkCapacityMonitor
                linkCapacityMonitor, Constellation constellation)
        {
            // AttackParams
            SourceGroundstations =
                attackParams.SrcGroundstations;
            Target = new AttackTarget(attackParams, sat0r, attack_radius, transform, prefab);
            _painter = painter;
            _linkCapacityMonitor = linkCapacityMonitor;
            _rg = rg;
            _constellation = constellation;
            _groundstations = groundstations;
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

        private BinaryHeap<Route> _FindAttackRoutes(RouteGraph rg, List<GameObject> dest_groundstations)
        {
            var heap = new BinaryHeap<Route>(
                dest_groundstations.Count * SourceGroundstations.Count); // priority queue <routegraph, routelength>
            foreach (var srcGs in SourceGroundstations)
            foreach (var destGs in dest_groundstations)
            {
                if (destGs == srcGs) continue;

                // Don't compute the route if the direction of the two nodes makes it unlikely that a possible
                // will exist.
                if (_IsInvalidPair(srcGs, destGs)) continue; 
                _rg.ResetRoute(srcGs, destGs, _painter, _constellation.satlist,
                    _constellation.maxsats);
                _rg.Build(srcGs, destGs, _constellation.maxdist,
                    _constellation.margin, _constellation.maxsats, _constellation.satlist,
                    _constellation.km_per_unit);
                rg.ComputeRoutes();
                var route = Route.Nodes2Route(rg.startnode, rg.endnode, srcGs, destGs);
                if (route != null && route.ContainsLink(Target.Link.SrcNode.Id, Target.Link.DestNode.Id))
                    heap.Add(route, (double)route.Nodes.Count);
            }

            return heap;
        }

        private void SimulateRoute(Route route, int mbits, LinkCapacityMonitor virtualLinkMonitor)
        {
            var rn = route.Nodes.Last();

            Debug.Assert(rn != null, "ExecuteAttackRoute | The last node is empty.");
            Debug.Assert(rn.Id == -1,
                $"Execute AttackRoute | The last node isn't -1. Instead, got {rn.Id}");

            if (route.Nodes.First().Id != -2) return; // This isn't a valid path; it doesn't have a destination.
            var previd = -4;
            var id = -4; /* first id */

            if (route.Nodes.Count == 1)
                // Only node -1 is present; so no real path exists.
                return;

            var index = route.Nodes.Count - 2;

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
                        if (virtualLinkMonitor.IsCongested(_groundstations[route.StartCity], id.ToString())) return;
                        virtualLinkMonitor.DecreaseLinkCapacity(_groundstations[route.StartCity], id.ToString(), mbits);
                    }
                    // The current node is a city and the previous, a satellite. (RF link)
                    else if (id == -2 && previd >= 0)
                    {
                        if (virtualLinkMonitor.IsCongested(previd.ToString(), _groundstations[route.EndCity])) return;
                        virtualLinkMonitor.DecreaseLinkCapacity(previd.ToString(), _groundstations[route.EndCity],
                            mbits);
                        break; // We've reached the end node. Time to exit the loop.
                    }
                }

                // Update referred links

                // If we've gone through the entire path, return.
                if (index == -1) return; // only show a path if it's feasible.

                rn = route.Nodes[index];
                if (rn == null) return; // only show a path if it's feasible.

                index--;
            }
        }

        /// Create routes until the link's capacity has supposedly been reached.
        private List<Route> FloodLink(BinaryHeap<Route> viableAttackRoutes, int mbits)
        {
            var virtualLinkCapacityMonitor = new LinkCapacityMonitor(_linkCapacityMonitor);
            var finalAttackRoutes = new List<Route>();

            while (!virtualLinkCapacityMonitor.IsCongested(Target.Link.SrcNode.Id, Target.Link.DestNode.Id) &&
                   viableAttackRoutes.Count > 0)
            {
                var route = viableAttackRoutes.ExtractMin();
                if (!route.HasEarlyCollisions(mbits,
                        virtualLinkCapacityMonitor, _groundstations[route.StartCity], Target.Link))
                {
                    SimulateRoute(route, mbits, virtualLinkCapacityMonitor);
                    finalAttackRoutes.Add(route);
                }
            }

            return finalAttackRoutes;
        }

        /// Execute the attacker object.
        public List<Route> Run(List<GameObject> groundstations)
        {
            _rg.ResetRoute(_groundstations["New York"], _groundstations["Toronto"], _painter,
                _constellation.satlist, _constellation.maxsats);
            _rg.Build(_groundstations["New York"], _groundstations["Toronto"],
                _constellation.maxdist, _constellation.margin, _constellation.maxsats,
                _constellation.satlist, _constellation.km_per_unit);

            // If the current link isn't valid, select a new target link.
            if (!Target.HasValidTargetLink())
                Target.ChangeTargetLink(_rg);

            // If the attacker has selected a valid link, attempt to attack it
            if (Target.Link != null && Target.HasValidTargetLink())
            {
                var viableAttackRoutes = _FindAttackRoutes(_rg, groundstations);
                return FloodLink(viableAttackRoutes, 4000); // send the maximum RF link capacity.
            }

            return new List<Route>();
        }
    }
}