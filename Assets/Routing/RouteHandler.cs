using System.Linq;
using Orbits.Satellites;
using Scene;
using UnityEngine;

namespace Routing
{
    public class RouteHandler // TODO: move to a separate class
    {
        // Should I create a new routehandler each time I create a route?
        private RouteGraph _rg;

        // TODO: add docstrings
        // public void ResetRoute(GameObject city1, GameObject city2, ScenePainter painter, Satellite[] satlist,
        //     int maxsats)
        // {
        //     _rg.ResetNodes(city1, city2);
        //     painter.TurnLasersOff(satlist, maxsats);
        // }


        /* Check if sending traffic through a given path will take down an earlier shared link in the network. Returns true if there is at least one early collision, and false otherwise. */
        // TODO: THIS FEELS LIKE A ROUTE FUNCTION TO BE FAIR :)
        public static bool RouteHasEarlyCollisionsOld(Route route, int desiredMbits, Node srcNode, Node destNode,
            LinkCapacityMonitor linkCapacities, string startCityName, string endCityName)
        {
            // REVIEW: I think this code might be going from the end node to the start node! I'm not sure
            var index = 0;
            var prevRn = route.Nodes.First();
            var rn = route.Nodes[index + 1];
            // TODO: I need to find a way to log RF link capacities as well. (format source city, destination satellite OR format source satellite, destination city)
            while (index < route.Nodes.Count)
            {
                // Target link is reachable. Exit the checker.
                if (prevRn == srcNode && rn == destNode) break;

                // TODO: move the city names into the path object too.
                // Check if an early link gets flooded.
                if (prevRn.Id == -2)
                {
                    if (linkCapacities.GetCapacity(startCityName, rn.Id.ToString()) - desiredMbits <
                        0) // RF link (source ground station, destination satellite)
                        return true;
                }
                else if (rn.Id == -1)
                {
                    if (linkCapacities.GetCapacity(prevRn.Id.ToString(), endCityName) - desiredMbits <
                        0) // RF link (source satellite, destination ground station)
                        return true;
                }
                else if (linkCapacities.GetCapacity(prevRn.Id, rn.Id) - desiredMbits < 0) // ISL link.
                {
                    return true;
                }

                prevRn = rn;
                rn = route.Nodes[index];
                index++;
            }

            return false;
        }



    }
}