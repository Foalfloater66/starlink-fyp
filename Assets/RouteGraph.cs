using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

public static class RouteHandler
{

	// TODO: add docstrings
	public static void InitRoute(RouteGraph rg, int maxsats, List<GameObject> relays, float maxdist, float km_per_unit)
	{
		rg.Init(maxsats, relays.Count, maxdist, km_per_unit);

		// Plus 2 for start and end city
		for (int satnum = 0; satnum < maxsats; satnum++)
		{
			rg.NewNode(satlist[satnum].satid, satlist[satnum].Orbit, satlist[satnum].gameobject);

		}
		rg.AddEndNodes();
		int relaycount = 0;
		foreach (GameObject relay in relays)
		{
			rg.NewNode(get_relay_id(relaycount), relay);
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

	public static RouteGraph BuildRouteGraph(RouteGraph rgph, GameObject city1, GameObject city2, float maxdist, float margin)
	{
		// TODO: Create a BuildRouteGraph function that doesn't include cities.

		for (int satnum = 0; satnum < maxsats; satnum++)
		{
			for (int i = 0; i < satlist[satnum].assignedcount; i++)
			{
				rgph.AddNeighbour(satnum, satlist[satnum].assignedsats[i].satid, false);
			}

			// Add start city
			float radiodist = Vector3.Distance(satlist[satnum].gameobject.transform.position,
				city1.transform.position);
			if (radiodist * km_per_unit < maxdist)
			{
				rgph.AddNeighbour(maxsats, satnum, radiodist, true);
			}
			else if (radiodist * km_per_unit < maxdist + margin)
			{
				rgph.AddNeighbour(maxsats, satnum, Node.INFINITY, true);
			}

			// Add end city
			radiodist = Vector3.Distance(satlist[satnum].gameobject.transform.position,
				city2.transform.position);
			if (radiodist * km_per_unit < maxdist)
			{
				rgph.AddNeighbour(maxsats + 1, satnum, radiodist, true);
			}
			else if (radiodist * km_per_unit < maxdist + margin)
			{
				rgph.AddNeighbour(maxsats + 1, satnum, Node.INFINITY, true);
			}

			// Add relays
			if (graph_on)
			{
				satlist[satnum].GraphReset();
			}

			List<List<City>> in_range = grid.FindInRange(satlist[satnum].gameobject.transform.position);
			foreach (List<City> lst in in_range)
			{
				foreach (City relay in lst)
				{
					radiodist = Vector3.Distance(satlist[satnum].gameobject.transform.position, relay.gameobject.transform.position);
					if (radiodist * km_per_unit < maxdist)
					{
						rgph.AddNeighbour(maxsats + 2 + relay.relayid, satnum, radiodist, true);
						if (graph_on)
						{
							satlist[satnum].GraphOn(relay.gameobject, null);
						}
					}
					else if (radiodist * km_per_unit < maxdist + margin)
					{
						rgph.AddNeighbour(maxsats + 2 + relay.relayid, satnum, Node.INFINITY, true);
					}
				}
			}
			if (graph_on)
			{
				satlist[satnum].GraphDone();
			}
		}
		return rgph;
	}

	// TODO: add docstrings
	public static void ResetRoute(GameObject city1, GameObject city2)
	{
		rg.ResetNodes(city1, city2);
		_painter.TurnLasersOff(satlist, maxsats);
	}

	/* Check if sending traffic through a given path will take down an earlier shared link in the network. Returns true if there is at least one early collision, and false otherwise. */
	// TODO: add docstrings
	public static bool RouteHasEarlyCollisions(Path path, int desired_mbits, Node src_node, Node dest_node, LinkCapacityMonitor _link_capacities)
	{
		int index = 3;
		Node prev_rn = path.nodes.First();
		Node rn = path.nodes[2];
		while (index < path.nodes.Count)
		{
			if (prev_rn == src_node && rn == dest_node) // we *want* this link to be flooded!
			{
				break;
			}
			if (_link_capacities.GetCapacity(prev_rn.Id, rn.Id) - desired_mbits < 0) // an early link would get flooded.
			{
				return true;
			}

			prev_rn = rn;
			rn = path.nodes[index];
			index++;
		}
		return false;
	}


	public static void LockRoute() // NB. This is never used for some reason, but I don't know why.
	{
		/* Basically maintain all of the used ISL links. */
		foreach (ActiveISL pair in _painter.UsedISLLinks)
		{
			pair.node1.LockLink(pair.node2);
			pair.node2.LockLink(pair.node1);
		}
		foreach (ActiveRF pair in _painter.UsedRFLinks)
		{
			pair.node1.LockLink(pair.node2);
			pair.node2.LockLink(pair.node1);
		}
	}
}

public class RouteGraph
{
	public Node[] nodes;
	GameObject[] objs;
	public Node startnode = null;
	public Node endnode = null;
	int nodecount = 0;
	int satcount = 0;
	public int rstcount = 0;
	float maxdist = 0f;
	float km_per_unit = 0f;
	BinaryHeap<Node> heap;

	System.IO.StreamWriter logfile;

	public GameObject StartObj
	{
		get
		{
			return objs[satcount];
		}
	}

	public GameObject EndObj
	{
		get
		{
			return objs[satcount + 1];
		}
	}

	public RouteGraph()
	{
	}

	public void Init(int maxsat, int maxrelay, float maxdist_, float km_per_unit_)
	{
		// if (log_choice != LogChoice.None)
		// {
		// logfile = new System.IO.StreamWriter(@log_filename); // gotta remove this after for safety.
		// }
		nodes = new Node[maxsat + 2 + maxrelay];
		objs = new GameObject[maxsat + 2 + maxrelay];
		nodecount = 0;
		maxdist = maxdist_;
		km_per_unit = km_per_unit_;
	}

	public int NodeCount
	{
		get
		{
			return nodecount;
		}
	}

	public void NewNode(int satid, int orbit, GameObject obj)
	{
		nodes[nodecount] = new Node(satid, obj.transform.position);
		nodes[nodecount].Orbit = orbit;
		objs[nodecount] = obj;
		nodecount++;
	}

	public void NewNode(int satid, GameObject obj)
	{
		/* Overridden version because nodes can be a satellite OR a relay (Idk what a relay is) */
		nodes[nodecount] = new Node(satid, obj.transform.position);
		objs[nodecount] = obj;
		nodecount++;
	}

	public Node GetNode(int satid)
	{
		for (int i = 0; i < nodecount; i++)
		{
			if (nodes[i].Id == satid)
			{
				return (nodes[i]);
			}
		}
		return null;
	}

	// only call AddEndNodes after you've added all the satellites
	public void AddEndNodes()
	{
		satcount = nodecount;
		nodes[nodecount] = new Node(-1, Vector3.zero);
		startnode = nodes[nodecount];
		startnode.Dist = 0f;
		nodecount++;
		nodes[nodecount] = new Node(-2, Vector3.zero);
		endnode = nodes[nodecount];
		nodecount++;
	}

	public void ResetEndpointNodes(GameObject startobj, GameObject endobj)
	{
		objs[satcount] = startobj;
		objs[satcount + 1] = endobj;
	}

	public void ResetNodes(GameObject startobj, GameObject endobj)
	{
		objs[satcount] = startobj;
		objs[satcount + 1] = endobj;
		for (int i = 0; i < nodecount; i++)
		{
			nodes[i].Reset(objs[i].transform.position);
		}
		startnode.Dist = 0f;
	}

	public void ResetNodesPos(GameObject startobj, GameObject endobj)
	{
		objs[satcount] = startobj;
		objs[satcount + 1] = endobj;
		for (int i = 0; i < nodecount; i++)
		{
			nodes[i].ResetPos(objs[i].transform.position);
		}
		for (int i = 0; i < nodecount; i++)
		{
			nodes[i].UpdateDists(maxdist / km_per_unit); // IT COULD BE THIS!!! THATS CAUSING A PROBLEM!!! maybe its making them unreachable, etc.
		}
		startnode.Dist = 0f;
	}

	public void ResetNodeDistances()
	{
		for (int i = 0; i < nodecount; i++)
		{
			nodes[i].Dist = Node.INFINITY;
		}
		startnode.Dist = 0f;
	}

	public void AddNeighbour(int nodenum1, int nodenum2, bool dist_limited)
	{
		nodes[nodenum1].AddNeighbour(nodes[nodenum2], dist_limited);
	}

	public void AddNeighbour(int nodenum1, int nodenum2, float dist, bool dist_limited)
	{
		/* adds a neighbour between the first node and the second node of a certain distance,
		where the distance can be limited. */
		nodes[nodenum1].AddNeighbour(nodes[nodenum2], dist, dist_limited);
	}


	public void ComputeRoutes()
	{
		/* Essentially runs Djikstra. */
		/* Make new binary heap. Add all of the nodes. */
		heap = new BinaryHeap<Node>(nodecount);
		for (int i = 0; i < nodecount; i++)
		{
			heap.Add(nodes[i], (double)nodes[i].Dist);
		}

		startnode.Dist = 0f;
		while (heap.Count > 0)
		{
			/* Extract the smallest Node n from the heap. */
			Node u = heap.ExtractMin();

			/* For the number of links associated with Node u: */
			for (int i = 0; i < u.LinkCount; i++)
			{
				/* Select a link of Node u. */
				Link l = u.GetLink(i);

				/* Get the neighbour of that link l. */
				Node n = u.GetNeighbour(l);

				/* Get the distance of that neighbour. */
				float dist = n.Dist;

				/* Compute the distance of Node u + link. */
				float newdist = u.Dist + l.Dist;

				/* If distance of neighbour n > new distance: */
				if (newdist < dist)
				{
					/* Update the new distance. */
					n.Dist = newdist;

					/* smallest node n's parent is u. */
					n.Parent = u;

					/* Decrease priority of that neighbour n. */
					heap.DecreasePriority(n, (double)newdist);
				}
			}
		}
	}

	public Node[] GetReachableNodes()
	{
		int reachablecount = 0;
		for (int i = 0; i < nodecount; i++)
		{
			if (nodes[i].Dist < Node.INFINITY)
			{
				reachablecount++;
			}
		}
		Node[] reachable = new Node[reachablecount];
		int ix = 0;
		for (int i = 0; i < nodecount; i++)
		{
			if (nodes[i].Dist < Node.INFINITY)
			{
				reachable[ix] = nodes[i];
				ix++;
			}
		}
		return reachable;
	}
}

class City
{
	public GameObject gameobject;
	public float lat;
	public float lng;
	public int relayid;
	public City(float lat_, float lng_, int relayid_, GameObject gameobj_)
	{
		lat = lat_;
		lng = lng_;
		relayid = relayid_;
		gameobject = gameobj_;
	}
}

class GroundRegion
{
	int lat, lng, size;
	public List<City> cities;
	public int citycount = 0;
	public GameObject centre;
	// params are edge of region, lat, lng are centre
	public GroundRegion(int lat_, int lng_, int size_, GameObject regioncentre, Transform earth_transform)
	{
		size = size_;
		lat = lat_ + size / 2;
		lng = lng_ + size / 2;
		cities = new List<City>();
		centre = new GameObject("name");
		centre.transform.position = new Vector3(0f, 0f, -6371f); //6371 = earth radius in km

		float long_offset = 20f;
		centre.transform.RotateAround(Vector3.zero, Vector3.up, lng - long_offset);
		Vector3 lat_axis = Quaternion.Euler(0f, -90f, 0f) * centre.transform.position;
		centre.transform.RotateAround(Vector3.zero, lat_axis, lat);
		centre.transform.SetParent(earth_transform, false);
	}


	public void AddCity(float lat, float lng, int relayid, GameObject city)
	{
		cities.Add(new City(lat, lng, relayid, city));
		citycount++;
	}
}

class GroundGrid
{
	int size;
	GroundRegion[,] regions;
	List<List<City>> in_range;
	int numregions;
	float maxdist, margin, squaremargin, km_per_unit;
	public int citycount = 0;

	public GroundGrid(int size_, float maxdist_, float margin_, float km_per_unit_, GameObject regioncentre, Transform earth_transform)
	{
		size = size_;
		maxdist = maxdist_;
		margin = margin_;
		km_per_unit = km_per_unit_;
		// 111 km per degree. size is in degrees.  0.7 is half of diagonal of a square (worst case)
		squaremargin = 0.7f * 111f * size;
		numregions = 360 / size;
		regions = new GroundRegion[numregions / 2, numregions]; // only 180 degrees n to s
		for (int lat = 0; lat < numregions / 2; lat++)
		{
			for (int lng = 0; lng < numregions; lng++)
			{
				int reallat = lat * size;
				if (lat * size > 90)
				{
					reallat = lat * size - 180;
				}
				int reallng = lng * size;
				if (lng * size > 180)
				{
					reallng = lng * size - 360;
				}
				regions[lat, lng] = new GroundRegion(reallat, reallng, size, regioncentre, earth_transform);
			}
		}
		in_range = new List<List<City>>();
	}

	public void AddCity(float lat, float lng, int relayid, GameObject city)
	{
		if (lat < 0)
			lat += 180f;
		if (lng < 0)
			lng += 360f;
		int ilat = (int)(lat / size);
		int ilng = (int)(lng / size);

		regions[ilat, ilng].AddCity(lat, lng, relayid, city);
		citycount++;
	}

	public List<List<City>> FindInRange(Vector3 pos)
	{
		in_range.Clear();
		float maxrange = (maxdist + margin + squaremargin) / km_per_unit;
		for (int lat = 0; lat < numregions / 2; lat++)
		{
			for (int lng = 0; lng < numregions; lng++)
			{
				if (regions[lat, lng].citycount > 0 &&
					Vector3.Distance(pos, regions[lat, lng].centre.transform.position) < maxrange)
				{
					in_range.Add(regions[lat, lng].cities);
				}
			}
		}
		return in_range;
	}


}
