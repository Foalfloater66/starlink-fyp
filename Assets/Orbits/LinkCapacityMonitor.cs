using System.Collections.Generic;
using System;

/* (node1, node2) link mapping to capacity. Links are full duplex. */
public class LinkCapacityMonitor
{

    private Dictionary<String, int> _capacities = new Dictionary<String, int>(); // contains capacities of added links.
    private int _initial_capacity; // initial capacity of any link in the network.

    public LinkCapacityMonitor(int initial_capacity)
    {
        _initial_capacity = initial_capacity;
    }

    public void Reset()
    {
        _capacities.Clear();
    }

    /* If a link doesn't exist in the dictionary, add it. */
    private void _AddMissingLink(string link_name)
    {
        if (!_capacities.ContainsKey(link_name))
        {
            _capacities.Add(link_name, _initial_capacity);
        }
    }

    /* Checks if a link is flooded. A link is flooded if its capacity is strictly under 0. */
    public bool IsFlooded(int src_id, int dest_id)
    {
        string link_name = src_id.ToString() + "-" + dest_id.ToString();
        _AddMissingLink(link_name);
        return (_capacities[link_name] < 0);
    }

    /* Decreases capacity of a link <src_id, dest_id> by mbits.*/
    public void DecreaseLinkCapacity(int src_id, int dest_id, int mbits)
    {
        string link_name = src_id.ToString() + "-" + dest_id.ToString();
        _AddMissingLink(link_name);
        _capacities[link_name] -= mbits;
    }

    public int GetCapacity(int src_id, int dest_id)
    {
        string link_name = src_id.ToString() + "-" + dest_id.ToString();
        _AddMissingLink(link_name);
        return _capacities[link_name];
    }
}