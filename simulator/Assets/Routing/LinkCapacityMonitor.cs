/* C138 Final Year Project 2023-2024 */

using System;
using System.Collections.Generic;
using Attack.Cases;

namespace Routing
{
    /// <summary>
    /// Class <c>LinkCapacityMonitor</c> maps links to their current capacity and provides methods to modify these capacities. Links are full duplex; they are unidirectional only. Therefore, for two nodes `a` and `b`, we have the following link property `(a, b) != `(b, a)`.
    /// A link's <c>capacity</c> is the number of additional mbits it can carry. If the capacity reaches 0, then the link is considered saturated. If the capacity is strictly below 0, then the link is considered flooded.
    /// The <c>LinkCapacityMonitor</c> provides capacity monitoring for both inter-satellite links (ISL) and radio frequency (RF) links.
    /// </summary>
    public class LinkCapacityMonitor : ICloneable
    {
        /// <value>
        /// Dictionary containing inter-satellite links with a set capacity.
        /// Keys are link names of format (src_id, dest_id) and values are integers in mbits.
        /// </value>
        private Dictionary<(int, int), int> _ISLcapacities;

        /// <summary>
        /// Dictionary containing radio frequency links with a set capacity.
        /// Keys are link names of format (src_groundstation, dest_id) or (src_id, dest_groundstation) and values are integers in mbits.
        /// </summary>
        private Dictionary<(string, string), int> _RFcapacities;

        /// <value>
        /// Initial maximum capacity of any link in the network.
        /// </value>
        private int _initialISLCapacity = 20000; // 20 Gbps

        private int _initialRFCapacity = 4000; // 4 Gbps

        /// <summary>
        /// Constructor initializing the <c>LinkCapacityMonitor</c> object and its`_initial_capacity` and `_capacities`.
        /// </summary>
        /// <param name="initial_capacity">Initial maximum capacity of any link in the network.</param>
        public LinkCapacityMonitor(CaseChoice attack)
        {
            if (attack == CaseChoice.SimpleDemo)
            {
                _initialISLCapacity = 8000; // 8 Gbps for demonstrative purposes.
            }

            _ISLcapacities = new Dictionary<(int, int), int>();
            _RFcapacities = new Dictionary<(string, string), int>();
        }

        public LinkCapacityMonitor(LinkCapacityMonitor monitor)
        {
            _ISLcapacities = new Dictionary<(int, int), int>(monitor._ISLcapacities);
            _RFcapacities = new Dictionary<(string, string), int>(monitor._RFcapacities);
        }

        /// <summary>
        /// Reset the capacity of all links to the original `_initial_capacity`.
        /// </summary>
        public void Reset()
        {
            _ISLcapacities.Clear();
            _RFcapacities.Clear();
        }

        /// <summary>
        /// If the ISL link doesn't exist in the link capacity dictionary, add it.
        /// </summary>
        /// <param name="src_id">Link source ID</param>
        /// <param name="dest_id">Link destination ID</param>
        private void _AddMissingLink(int src_id, int dest_id)
        {
            if (!_ISLcapacities.ContainsKey((src_id, dest_id)))
                _ISLcapacities.Add((src_id, dest_id), _initialISLCapacity);
        }

        /// <summary>
        /// If the RF link doesn't exist in the link capacity dictionary, add it.
        /// </summary>
        /// <param name="src_name">Link source groundstation name/satellite ID</param>
        /// <param name="dest_name">Link destination groundstation name/satellite ID</param>
        private void _AddMissingLink(string src_name, string dest_name)
        {
            if (!_RFcapacities.ContainsKey((src_name, dest_name)))
                _RFcapacities.Add((src_name, dest_name), _initialRFCapacity);
        }

        /// <summary>
        /// Checks if an ISL link is flooded. A link is flooded if its capacity is strictly under 0. 
        /// </summary>
        /// <param name="src_id">Link source ID</param>
        /// <param name="dest_id">Link destination ID</param>
        /// <returns>True if the link is flooded. False otherwise.</returns>
        public bool IsCongested(int src_id, int dest_id)
        {
            _AddMissingLink(src_id, dest_id);
            return _ISLcapacities[(src_id, dest_id)] <= 0;
        }

        /// <summary>
        /// Checks if an RF link is flooded. A link is flooded if its capacity is strictly under 0.
        /// </summary>
        /// <param name="src_name">Link source groundstation name/satellite ID</param>
        /// <param name="dest_name">Link destination groundstation name/satellite ID</param>
        /// <returns>True if the RF link is flooded. False otherwise.</returns>
        public bool IsCongested(string src_name, string dest_name)
        {
            _AddMissingLink(src_name, dest_name);
            return _RFcapacities[(src_name, dest_name)] <= 0;
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
            _ISLcapacities[(src_id, dest_id)] -= mbits;
        }

        public void DecreaseLinkCapacity(string src_name, string dest_name, int mbits)
        {
            _AddMissingLink(src_name, dest_name);
            _RFcapacities[(src_name, dest_name)] -= mbits;
        }

        /// <summary>
        /// Get the capacity of an ISL link.
        /// </summary>
        /// <param name="src_id">Link source ID</param>
        /// <param name="dest_id">Link destination ID</param>
        /// <returns>Capacity of the link.</returns>
        public int GetCapacity(int src_id, int dest_id)
        {
            _AddMissingLink(src_id, dest_id);
            return _ISLcapacities[(src_id, dest_id)];
        }

        /// <summary>
        /// Get the capacity of an RF link.
        /// </summary>
        /// <param name="src_name">Identifier of the source groundstation (name) or satellite (id)</param>
        /// <param name="dest_name">Identifier of the destination groundstation (name) or satellite (id)</param>
        /// <returns></returns>
        public int GetCapacity(string src_name, string dest_name)
        {
            _AddMissingLink(src_name, dest_name);
            return _RFcapacities[(src_name, dest_name)];
        }

        public object Clone()
        {
            return new LinkCapacityMonitor(this);
        }
    }
}