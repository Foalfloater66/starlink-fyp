using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Routing
{
 public class Node : HeapNode
    {
        private int _id;
        public Vector3 _position;
        private Link[] _links;
        private int _linkcount = 0;
        public const float INFINITY = 1000000f;

        private int _orbit = -5;
        private float _dist = INFINITY; // distance from src.
        private Node _parent_node; // predecessor on path from src

        private Dictionary<int, Link>
            _neighbours = new Dictionary<int, Link>(); // all node neighbours and their respective links.

        public Node(int id, Vector3 pos)
        {
            _id = id;
            _position = pos;
            _links = new Link[2000];
            _linkcount = 0;
        }

        public void Reset(Vector3 position)
        {
            for (var i = 0; i < _linkcount; i++) _links[i] = null;
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
            for (var i = 0; i < _linkcount; i++) _links[i].UpdateDist(maxdist);
        }


        /// <summary>
        /// Lock the currently used links.
        /// </summary>
        /// <param name="peer">Node at other end of link to lock</param>
        public void LockLink(Node peer)
        {
            for (var i = 0; i < _linkcount; i++)
                if (_links[i].OtherNode(this) == peer)
                    _links[i].Dist = INFINITY;
        }

        public int Id => _id;

        public Vector3 Position => _position;

        public int Orbit
        {
            get => _orbit;
            set => _orbit = value;
        }

        public float Dist
        {
            get => _dist;
            set => _dist = value;
        }

        public Node Parent
        {
            get => _parent_node;
            set => _parent_node = value;
        }

        private void AddLink(Link l)
        {
            _links[_linkcount++] = l;
        }

        public int LinkCount => _linkcount;

        public void AddNeighbour(Node node, bool dist_limited)
        {
            for (var i = 0; i < _linkcount; i++)
                if (_links[i].OtherNode(this) == node)
                    // this one is already a neighbour
                    return;
            var l = new Link(this, node, dist_limited);
            AddLink(l);
            node.AddLink(l);
            // _neighbours.Add(node.Id, l);
        }

        public void AddNeighbour(Node node, float dist, bool dist_limited)
        {
            /* check all of the other links in _linkcount, and check if the
        node we seek to add as a neighbour already has a neighbour.
        If yes, return. */
            for (var i = 0; i < _linkcount; i++)
                if (_links[i].OtherNode(this) == node)
                    // this one is already a neighbour
                    return;
            /* Otherwise, create a link between our node and the desired neighbour. */
            var l = new Link(this, node, dist, dist_limited); /* create a link object with the distance data. */
            AddLink(l); /* add the link to our node */
            node.AddLink(l); /* add the link to the desired neighbour */
            // _neighbours.Add(node.Id, l);
        }

        public Node GetNeighbour(Link l)
        {
            var n = l.OtherNode(this);
            return n;
        }

        public Link GetLinkByNeighbour(int id)
        {
            // return _neighbours[satid];
            for (var i = 0; i < _linkcount; i++)
                if (_links[i].OtherNode(this).Id == id)
                    // this is the correct neighbour
                    return _links[i];
            return null;
        }

        public Link GetLink(int index)
        {
            return _links[index];
        }
    }
}