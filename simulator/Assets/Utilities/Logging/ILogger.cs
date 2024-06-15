using System.Collections.Generic;
using Attack;
using Routing;

namespace Utilities.Logging
{
    public interface ILogger
    {
        void LogEntry(int framecount, LoggingContext ctx); 

        void Save();
    }
}
