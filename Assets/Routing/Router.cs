using System.Collections.Generic;
using System.IO;
using System.Linq;
using Attack;
using Orbits;
using Orbits.Satellites;
using Scene;
using UnityEngine;
using UnityEngine.Assertions;

namespace Routing
{
    /**
    * @author Morgane Marie Ohlig
    */
    /// <summary>
    /// ISN routing system. Does the in-system routing.
    /// </summary>
    public class Router
    {
        private bool _defenceOn = false;
        private float _kmPerUnit;

        // TODO: I need to check what I actually need to keep here.
        private RouteHandler _routeHandler;
        private ScenePainter _painter;
        private LinkCapacityMonitor _linkCapacityMonitor;
        private GroundstationCollection _Groundstations;

        private RouteGraph _rg;

        // public AttackTarget Target;
        private Constellation _constellation;

        /// <summary>
        /// File for debugging.
        /// </summary>
        // private System.IO.StreamWriter logfile;
        private StreamWriter _attackLogger;

        public Router(bool defenceOn,GroundstationCollection groundstations, RouteHandler route_handler, ScenePainter painter,
            LinkCapacityMonitor
                linkCapacityMonitor, Constellation constellation, StreamWriter fileWriter, float kmPerUnit)
        {
            _defenceOn = defenceOn;
            Assert.IsTrue(_kmPerUnit >= 0);
            _kmPerUnit = kmPerUnit;
            _painter = painter;
            _linkCapacityMonitor = linkCapacityMonitor;
            _routeHandler = route_handler;
            _constellation = constellation;

            _Groundstations = groundstations;
            _attackLogger = fileWriter;
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
            // TODO: I want to put this in the Path thing.
            // This function processes the route in the reverse order.
            // TODO: this EXTRACTS THE attack route.

            // System.Diagnostics.Debug.Assert(Target.Link != null, "FindAttackRoute | No attacker link has been set.");

            var rn = endNode;
            var route = new Path(startNode, endNode, srcGs, destGs, 4000);
            route.Nodes.Add(rn);

            var endsatid = -1;
            var id = -4;

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
                rn = rn.Parent;
                if (rn == null) // this route is incomplete.
                    return null;
            }

            return route;
        }


        // TODO: I would like to put this in the defence code area. or like. some routing code.
        public Path GetRandomRoute(Node startNode, Node endNode, GameObject srcGs, GameObject destGs,
            int rmax)
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
                    _routeHandler.ResetRoute(srcGs, destGs, _painter, _constellation.satlist,
                        _constellation.maxsats);
                    _rg = _routeHandler.BuildRouteGraph(srcGs, destGs, _constellation.maxdist,
                        _constellation.margin, _constellation.maxsats, _constellation.satlist,
                        _constellation.km_per_unit);
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

        /* Draw the computed path and send traffic in mbits. */
        public void ExecuteAttackRoute(Path path, GameObject city1 /* TODO: remove this */,
            GameObject city2 /* TODO: remove this */, int mbits, LinkCapacityMonitor _linkCapacityMonitor,
            ScenePainter _painter, Constellation constellation_ctx)
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

        // TODO: I want any painting-related functionality to be done later on.
        /// <summary>
        /// Update the Target Link Visuals
        /// </summary>
        private void UpdateTargetLinkVisuals(Constellation constellation_ctx, AttackTarget Target)
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


        public void Run(List<Path> routes, AttackTarget Target)
        {

            // Execute the final list of attack routes.
            foreach (var route in routes)
            {
                var executedRoute = route;

                if (_defenceOn)
                    executedRoute = GetRandomRoute(route.StartNode, route.EndNode, route.StartCity,
                        route.EndCity, 3);

                // executedRoute.
                ExecuteAttackRoute(executedRoute, route.StartCity, route.EndCity, 4000, _linkCapacityMonitor,
                    _painter, _constellation);

                // counter++;
            }

            if (Target.Link != null)
            {
                UpdateTargetLinkVisuals(_constellation, Target); // TODO: can move this outside? idk.
                _attackLogger.Write(
                    $",{_linkCapacityMonitor.GetCapacity(Target.Link.SrcNode.Id, Target.Link.DestNode.Id)}");
                
            }
        }
    }
}