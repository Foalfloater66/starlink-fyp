using System.Collections.Generic;
using Attack;
using Routing;

namespace Utilities.Logging
{
    public struct LoggingContext
    {
        public AttackTarget Target { get; set;}
        public List<Route> Routes { get; set;}
        public List<float> RTT { get; set;}
    }
}

