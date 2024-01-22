using UnityEngine;
using System.Collections;



public class Link
{
	Node[] nodes;
	float dist; // length of link
	bool dist_limited;  // RF links have a distance limit, lasers do not
	public Link(Node n1, Node n2, bool dist_limited_)
	{
		nodes = new Node[2];
		nodes[0] = n1;
		nodes[1] = n2;
		dist = Vector3.Distance(n1._position, n2._position);
		dist_limited = dist_limited_;
	}

	public Link(Node n1, Node n2, float dist_, bool dist_limited_)
	{
		nodes = new Node[2];
		nodes[0] = n1;
		nodes[1] = n2;
		dist = dist_;
		dist_limited = dist_limited_;
	}

	public Node OtherNode(Node n)
	{
		if (nodes[0] == n)
		{
			return nodes[1];
		}
		Debug.Assert(nodes[1] == n);
		return nodes[0];
	}

	public float Dist
	{
		get
		{
			return dist;
		}
		set
		{
			dist = value;
		}
	}

	public void UpdateDist(float maxdist)
	{
		dist = Vector3.Distance(nodes[0]._position, nodes[1]._position);
		if (dist_limited && dist > maxdist)
		{
			dist = Node.INFINITY; // unreachable
		}
	}
}

public class Node
{
	int _id;
	public Vector3 _position;
	Link[] _links;
	int _linkcount = 0;
	public const float INFINITY = 1000000f;

	public int _orbit = -5; // just means an orbit hasn't been assigned. I don't know if -5 is used for it. Also it would frankly be better to just have a macro or smth.
	float _dist = INFINITY; // distance from src.
	Node _parent_node;  // predecessor on path from src
						//bool _done = false;
	public int QueuePosition;

	public Node(int id, Vector3 pos)
	{
		_id = id;
		_position = pos;
		_links = new Link[2000];
		_linkcount = 0;
	}

	public void Reset(Vector3 position)
	{
		for (int i = 0; i < _linkcount; i++)
		{
			_links[i] = null;
		}
		_linkcount = 0;
		_position = position;
		_dist = INFINITY;
		_parent_node = null;
	}

	public void ResetPos(Vector3 position)
	{
		_position = position;
		_dist = INFINITY;
		_parent_node = null;
	}

	public void UpdateDists(float maxdist)
	{
		for (int i = 0; i < _linkcount; i++)
		{
			_links[i].UpdateDist(maxdist);
		}
	}


	/// <summary>
	/// Lock the currently used links.
	/// </summary>
	/// <param name="peer">Node at other end of link to lock</param>
	public void LockLink(Node peer)
	{
		/* What does this mean. */
		for (int i = 0; i < _linkcount; i++)
		{
			if (_links[i].OtherNode(this) == peer)
			{
				_links[i].Dist = INFINITY;
			}
		}
	}

	public int Id
	{
		get
		{
			return _id;
		}
	}

	public int Orbit
	{
		get
		{
			return _orbit;
		}
		set
		{
			_orbit = value;
		}
	}


	public float Dist
	{
		get
		{
			return _dist;
		}
		set
		{
			_dist = value;
		}
	}

	public Node Parent
	{
		get
		{
			return _parent_node;
		}
		set
		{
			_parent_node = value;
		}
	}

	void AddLink(Link l)
	{
		_links[_linkcount++] = l;
	}

	public int LinkCount
	{
		get
		{
			return _linkcount;
		}
	}

	public void AddNeighbour(Node node, bool dist_limited)
	{
		for (int i = 0; i < _linkcount; i++)
		{
			if (_links[i].OtherNode(this) == node)
			{
				// this one is already a neighbour
				return;
			}
		}
		Link l = new Link(this, node, dist_limited);
		AddLink(l);
		node.AddLink(l);
	}

	public void AddNeighbour(Node node, float dist, bool dist_limited)
	{
		/* check all of the other links in _linkcount, and check if the
		node we seek to add as a neighbour already has a neighbour. 
		If yes, return. */
		for (int i = 0; i < _linkcount; i++)
		{
			if (_links[i].OtherNode(this) == node)
			{
				// this one is already a neighbour
				return;
			}
		}
		/* Otherwise, create a link between our node and the desired neighbour. */
		Link l = new Link(this, node, dist, dist_limited); /* create a link object with the distance data. */
		AddLink(l); /* add the link to our node */
		node.AddLink(l); /* add the link to the desired neighbour */
	}

	public Node GetNeighbour(Link l)
	{
		Node n = l.OtherNode(this);
		return n;
	}

	public Link GetLink(int index)
	{
		return _links[index];
	}
}

