using System.Collections.Generic;
using Attack;
using Routing;

namespace Logging
{
    public interface ILogger
    {
        void LogEntry(int frameCount, AttackTarget target, List<Route> routes);
    }
}
