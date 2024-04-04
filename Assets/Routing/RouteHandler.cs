using System.Collections.Generic;
using System.Linq;
using Orbits;
using Orbits.Satellites;
using Scene;
using UnityEngine;

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

	public RouteGraph BuildRouteGraph(GameObject city1, GameObject city2, float maxdist, float margin, int maxsats, Satellite[] satlist, float kmPerUnit, bool graphOn, GroundGrid grid)
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

			List<List<City>> inRange = grid.FindInRange(satlist[satnum].gameobject.transform.position);
			foreach (List<City> lst in inRange)
			{
				foreach (City relay in lst)
				{
					radiodist = Vector3.Distance(satlist[satnum].gameobject.transform.position, relay.gameobject.transform.position);
					if (radiodist * kmPerUnit < maxdist)
					{
						_rg.AddNeighbour(maxsats + 2 + relay.relayid, satnum, radiodist, true);
						if (graphOn)
						{
							satlist[satnum].GraphOn(relay.gameobject, null);
						}
					}
					else if (radiodist * kmPerUnit < maxdist + margin)
					{
						_rg.AddNeighbour(maxsats + 2 + relay.relayid, satnum, Node.INFINITY, true);
					}
				}
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

	/* Check if sending traffic through a given path will take down an earlier shared link in the network. Returns true if there is at least one early collision, and false otherwise. */
	// TODO: add docstrings
	public static bool RouteHasEarlyCollisions(Path path, int desiredMbits, Node srcNode, Node destNode, LinkCapacityMonitor linkCapacities)
	{
		int index = 3;
		Node prevRn = path.nodes.First();
		Node rn = path.nodes[2];
		while (index < path.nodes.Count)
		{
			if (prevRn == srcNode && rn == destNode) // we *want* this link to be flooded!
			{
				break;
			}
			if (linkCapacities.GetCapacity(prevRn.Id, rn.Id) - desiredMbits < 0) // an early link would get flooded.
			{
				return true;
			}

			prevRn = rn;
			rn = path.nodes[index];
			index++;
		}
		return false;
	}


	public static void LockRoute(ScenePainter painter) // NB. This is never used for some reason, but I don't know why.
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