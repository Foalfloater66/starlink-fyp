using System.Collections.Generic;
using System.Linq;
using Attack;
using Orbits;
using Orbits.Satellites;
using Scene;
using UnityEngine;
using UnityEngine.Assertions;

namespace Routing
{
    /// <summary>
    /// ISN routing system. Does the in-system routing.
    /// </summary>
    public class Router
    {
        private bool _defenceOn;
        private float _kmPerUnit;
        private ScenePainter _painter;
        private LinkCapacityMonitor _linkCapacityMonitor;
        private GroundstationCollection _Groundstations;
        private RouteGraph _rg;
        private Constellation _constellation;

        public Router(bool defenceOn,GroundstationCollection groundstations, RouteGraph rg, ScenePainter painter,
            LinkCapacityMonitor
                linkCapacityMonitor, Constellation constellation, float kmPerUnit)
        {
            _defenceOn = defenceOn;
            Assert.IsTrue(_kmPerUnit >= 0);
            _kmPerUnit = kmPerUnit;
            _painter = painter;
            _rg = rg;
            _linkCapacityMonitor = linkCapacityMonitor;
            _constellation = constellation;
            _Groundstations = groundstations;
        }

        private Route GetRandomRoute(Node startNode, Node endNode, GameObject srcGs, GameObject destGs,
            int rmax)
        {
            // TODO: check what the randomizer uses.
            var selectedRouteId = new System.Random().Next(0, rmax);

            // Compute the multiple shortest paths and select the one for the selected random ID.
            for (var i = 0; i < rmax; i++)
            {
                // Update the route graph in a multipath setting.
                if (i == 0)
                {
                    _rg.ResetRoute(srcGs, destGs, _painter, _constellation.satlist,
                        _constellation.maxsats);
                    _rg.Build(srcGs, destGs, _constellation.maxdist,
                        _constellation.margin, _constellation.maxsats, _constellation.satlist,
                        _constellation.km_per_unit);
                }
                else
                {
                    _rg.ResetNodeDistances();
                }

                _rg.ComputeRoutes();

                if (i == selectedRouteId) return Route.Nodes2Route(startNode, endNode, srcGs, destGs); 
                    // ExtractRoute(startNode, endNode, srcGs, destGs);

                _rg.LockRoute(_painter);
            }

            return null;
        }

        /// <summary>
        /// Draw the computed path and send traffic in mbits.
        /// </summary>
        private void ExecuteAttackRoute(Route route, GameObject city1 /* TODO: remove this */,
            GameObject city2 /* TODO: remove this */, int mbits, LinkCapacityMonitor _linkCapacityMonitor,
            ScenePainter _painter, Constellation constellation_ctx)
        {
            var rn = route.Nodes.Last();

            Debug.Assert(rn != null); //, "ExecuteAttackRoute | The last node is empty.");
            Debug.Assert(rn.Id == -1,
                $"Execute AttackRoute | The last node isn't -1. Instead, got {rn.Id}"); //, "ExecuteAttackRoute | The last node is not -2. Instead, it's " + rn.Id);

            if (route.Nodes.First().Id != -2) return; // This isn't a valid path; it doesn't have a destination.
            Node prevnode = null;
            Satellite sat = null;
            Satellite prevsat = null;
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
                        sat = constellation_ctx.satlist[id];
                        prevsat = constellation_ctx.satlist[previd];

                        // Increase the load of the link and abort if link becomes flooded.
                        if (_linkCapacityMonitor.IsCongested(previd, id)) return;
                        _linkCapacityMonitor.DecreaseLinkCapacity(previd, id, mbits);
                        _painter.ColorRouteISLLink(prevsat, sat, prevnode, rn);
                    }
                    // The current node is a satellite and the previous, a city. (RF link)
                    else if (id >= 0 && previd == -1)
                    {
                        if (_linkCapacityMonitor.IsCongested(_Groundstations[route.StartCity], id.ToString())) return;
                        _linkCapacityMonitor.DecreaseLinkCapacity(_Groundstations[route.StartCity], id.ToString(),
                            mbits);
                        sat = constellation_ctx.satlist[id];
                        _painter.ColorRFLink(city1, sat, prevnode, rn);
                    }
                    // The current node is a city and the previous, a satellite. (RF link)
                    else if (id == -2 && previd >= 0)
                    {
                        if (_linkCapacityMonitor.IsCongested(previd.ToString(), _Groundstations[route.EndCity])) return;
                        _linkCapacityMonitor.DecreaseLinkCapacity(previd.ToString(), _Groundstations[route.EndCity],
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

                rn = route.Nodes[index];
                if (rn == null) // only show a path if it's feasible.
                    return;

                index--;
            }
        }

        /// <summary>
        /// Update the Target Link Visuals
        /// </summary>
        private void UpdateTargetLinkVisuals(Constellation constellationCtx, AttackTarget Target)
        {
            // Debugging messages.
            // Color the target link on the map. If flooded, the link is colored red. Otherwise, the link is colored pink.
            // Checks if the link has any capacity left
            if (_linkCapacityMonitor.IsCongested(Target.Link.SrcNode.Id, Target.Link.DestNode.Id))
                _painter.ColorTargetISLLink(constellationCtx.satlist[Target.Link.SrcNode.Id],
                    constellationCtx.satlist[Target.Link.DestNode.Id], Target.Link.DestNode, Target.Link.SrcNode,
                    true);
            else
                _painter.ColorTargetISLLink(constellationCtx.satlist[Target.Link.SrcNode.Id],
                    constellationCtx.satlist[Target.Link.DestNode.Id], Target.Link.DestNode, Target.Link.SrcNode,
                    false);
        }


        public void Run(List<Route> routes, AttackTarget target)
        {

            // Execute the final list of attack routes.
            foreach (var route in routes)
            {
                var executedRoute = route;
                if (_defenceOn)
                    executedRoute = GetRandomRoute(route.StartNode, route.EndNode, route.StartCity,
                        route.EndCity, 3);
                ExecuteAttackRoute(executedRoute, route.StartCity, route.EndCity, 4000, _linkCapacityMonitor,
                    _painter, _constellation);
            }

            if (target.Link != null)
            {
                UpdateTargetLinkVisuals(_constellation, target); // TODO: can move this outside? idk.

            }
        }
    }
}