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
public class RouteHandler // TODO: move to a separate class
{
	// Should I create a new routehandler each time I create a route?
	private RouteGraph _rg;
	private ScenePainter _painter;


	public RouteHandler(ScenePainter painter)
	{
		// this.rg = rg;
		_rg = new RouteGraph();
		_painter = painter;
	}
	
	// TODO: Can I move this somewhere else?
	static int get_relay_id(int relaynum)
	{
		return ((-1000) - relaynum);
	}

	// TODO: add docstrings
	public void InitRoute(int maxsats, Satellite[] satlist, List<GameObject> relays, float maxdist, float kmPerUnit)
	{
		_rg.Init(maxsats, relays.Count, maxdist, kmPerUnit);

		// Plus 2 for start and end city
		for (int satnum = 0; satnum < maxsats; satnum++)
		{
			_rg.NewNode(satlist[satnum].satid, satlist[satnum].Orbit, satlist[satnum].gameobject);

		}
		_rg.AddEndNodes();
		int relaycount = 0;
		foreach (GameObject relay in relays)
		{
			_rg.NewNode(get_relay_id(relaycount), relay);
			relaycount++;
		}
	}

	/// <summary>
	/// Remove all of the used ISL and RF links from the routegraph.
	/// </summary>
	public static void ClearRoutes(ScenePainter painter)
	{
		painter.EraseAllISLLinks();
		painter.EraseAllRFLinks();
	}

	public RouteGraph BuildRouteGraph(GameObject city1, GameObject city2, float maxdist, float margin, int maxsats, Satellite[] satlist, float kmPerUnit, bool graphOn)
	{
		// TODO: Create a BuildRouteGraph function that doesn't include cities.

		for (int satnum = 0; satnum < maxsats; satnum++)
		{
			for (int i = 0; i < satlist[satnum].assignedcount; i++)
			{
				_rg.AddNeighbour(satnum, satlist[satnum].assignedsats[i].satid, false);
			}

			// Add start city
			float radiodist = Vector3.Distance(satlist[satnum].gameobject.transform.position,
				city1.transform.position);
			if (radiodist * kmPerUnit < maxdist)
			{
				_rg.AddNeighbour(maxsats, satnum, radiodist, true);
			}
			else if (radiodist * kmPerUnit < maxdist + margin)
			{
				_rg.AddNeighbour(maxsats, satnum, Node.INFINITY, true);
			}

			// Add end city
			radiodist = Vector3.Distance(satlist[satnum].gameobject.transform.position,
				city2.transform.position);
			if (radiodist * kmPerUnit < maxdist)
			{
				_rg.AddNeighbour(maxsats + 1, satnum, radiodist, true);
			}
			else if (radiodist * kmPerUnit < maxdist + margin)
			{
				_rg.AddNeighbour(maxsats + 1, satnum, Node.INFINITY, true);
			}

			// Add relays
			if (graphOn)
			{
				satlist[satnum].GraphReset();
			}
			if (graphOn)
			{
				satlist[satnum].GraphDone();
			}
		}
		return _rg;
	}

	// TODO: add docstrings
	public void ResetRoute(GameObject city1, GameObject city2, ScenePainter painter, Satellite[] satlist, int maxsats)
	{
		_rg.ResetNodes(city1, city2);
		painter.TurnLasersOff(satlist, maxsats);
	}
	
	// TODO: add docstring
	public void ResetRoutePos(GameObject city1, GameObject city2, ScenePainter painter, Satellite[] satlist, int maxsats) {
		_rg.ResetNodesPos(city1, city2);
		painter.TurnLasersOff(satlist, maxsats);
	}

	/* Check if sending traffic through a given path will take down an earlier shared link in the network. Returns true if there is at least one early collision, and false otherwise. */
	// TODO: add docstrings
	public static bool RouteHasEarlyCollisions(Path path, int desiredMbits, Node srcNode, Node destNode, LinkCapacityMonitor linkCapacities, string startCityName, string endCityName)
	{
		// REVIEW: I think this code might be going from the end node to the start node! I'm not sure
		int index = 0;
		Node prevRn = path.nodes.First();
		Debug.Assert(path.nodes.Count > 2);
		Node rn = path.nodes[index+1];
		// TODO: I need to find a way to log RF link capacities as well. (format source city, destination satellite OR format source satellite, destination city)
		Debug.Log($"RouteHasEarlyCollision: Node that I start checking this stuff at {rn.Id}");
		while (index < path.nodes.Count)
		{
			// Target link is reachable. Exit the checker.
			if (prevRn == srcNode && rn == destNode) 
			{
				break;
			}

			// TODO: move the city names into the path object too.
			// Check if an early link gets flooded.
			if (prevRn.Id == -2)
			{
				if (linkCapacities.GetCapacity(startCityName, rn.Id.ToString()) - desiredMbits < 0) // RF link (source ground station, destination satellite)
				{
					return true;
				}
			}
			else if (rn.Id == -1)
			{
				if (linkCapacities.GetCapacity(prevRn.Id.ToString(), endCityName) - desiredMbits < 0) // RF link (source satellite, destination ground station)
				{
					return true;
				}
			}
			else if (linkCapacities.GetCapacity(prevRn.Id, rn.Id) - desiredMbits < 0) // ISL link.
			{
				return true;
			}

			prevRn = rn;
			rn = path.nodes[index];
			index++;
		}
		return false;
	}


	public static void LockRoute(ScenePainter painter) // REVIEW: This is never used for some reason, but I don't know why.
	{
		/* Basically maintain all of the used ISL links. */
		foreach (ActiveISL pair in painter.UsedISLLinks)
		{
			pair.node1.LockLink(pair.node2);
			pair.node2.LockLink(pair.node1);
		}
		foreach (ActiveRF pair in painter.UsedRFLinks)
		{
			pair.node1.LockLink(pair.node2);
			pair.node2.LockLink(pair.node1);
		}
	}
}
}