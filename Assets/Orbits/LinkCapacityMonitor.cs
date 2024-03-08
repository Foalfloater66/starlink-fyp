using System.Collections.Generic;
using System;

/// <summary>
/// Class LinkCapacityMonitor maps links to their current capacity and provides methods to modify these capacities. Links are full duplex; they are unidirectional only. Therefore, for two nodes `a` and `b`, we have the following link property `(a, b) != `(b, a)`.
/// A link's <c>capacity</c> is the number of additional mbits it can carry. If the capacity reaches 0, then the link is considered saturated. If the capacity is strictly below 0, then the link is considered flooded.
/// </summary>
public class LinkCapacityMonitor
{

    /// <value>
    /// Dictionary containing links with a set capacity. Keys are link names of format (src_id, dest_id) and values are integers in mbits.
    /// </value>
    private Dictionary<(int, int), int> _capacities;

    /// <value>
    /// Initial maximum capacity of any link in the network.
    /// </value>
    private int _initial_capacity;

    /// <summary>
    /// Constructor initializing the <c>LinkCapacityMonitor</c> object and its`_initial_capacity` and `_capacities`.
    /// </summary>
    /// <param name="initial_capacity">Initial maximum capacity of any link in the network.</param>
    public LinkCapacityMonitor(int initial_capacity)
    {
        _capacities = new Dictionary<(int, int), int>();
        _initial_capacity = initial_capacity;
    }

    /// <summary>
    /// Reset the capacity of all links to the original `_initial_capacity`.
    /// </summary>
    public void Reset()
    {
        _capacities.Clear();
    }

    /// <summary>
    /// If the link doesn't exist in the link capacity dictionary, add it.
    /// </summary>
    /// <param name="src_id">Link source ID</param>
    /// <param name="dest_id">Link destination ID</param>
    private void _AddMissingLink(int src_id, int dest_id)
    {
        if (!_capacities.ContainsKey((src_id, dest_id)))
        {
            _capacities.Add((src_id, dest_id), _initial_capacity);
        }
    }

    /// <summary>
    /// Checks if a link (`src_id`, `dest_id`) is flooded. A link is flooded if its capacity is strictly under 0. 
    /// </summary>
    /// <param name="src_id">Link source ID</param>
    /// <param name="dest_id">Link destination ID</param>
    /// <returns>True if link is flooded. False otherwise.</returns>
    public bool IsFlooded(int src_id, int dest_id)
    {
        _AddMissingLink(src_id, dest_id);
        return (_capacities[(src_id, dest_id)] < 0);
    }

    /// <summary>
    /// Decreases capacity of a link by mbits.
    /// </summary>
    /// <param name="src_id">Link source ID</param>
    /// <param name="dest_id">Link destination ID</param>
    /// <param name="mbits">Number of mbits to send through the link</param>
    public void DecreaseLinkCapacity(int src_id, int dest_id, int mbits)
    {
        _AddMissingLink(src_id, dest_id);
        _capacities[(src_id, dest_id)] -= mbits;
    }

    /// <summary>
    /// Get the capacity of a link.
    /// </summary>
    /// <param name="src_id">Link source ID</param>
    /// <param name="dest_id">Link destination ID</param>
    /// <returns>Capacity of the link.</returns>
    public int GetCapacity(int src_id, int dest_id)
    {
        _AddMissingLink(src_id, dest_id);
        return _capacities[(src_id, dest_id)];
    } // TODO: change string names to tuples.
}