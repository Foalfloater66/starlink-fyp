﻿using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Routing
{
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
			nodes[nodecount] = new Node(-1, Vector3.zero);	// Start node.
			startnode = nodes[nodecount];
			startnode.Dist = 0f;
			nodecount++;
			nodes[nodecount] = new Node(-2, Vector3.zero);	// End node.
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
			startnode.Dist = 0f; // TODO: maybe I should use this instead of ResetNodes.
		}

		public void ResetNodeDistances()
		{
			for (int i = 0; i < nodecount; i++)
			{
				nodes[i].Dist = Node.INFINITY;
			}
			startnode.Dist = 0f;
		} // TODO: maybe I should use this as well right after?

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
			/* Essentially runs Dijkstra. */
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
}