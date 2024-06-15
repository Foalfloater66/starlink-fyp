using System;
using System.IO;
using Orbits;
using Routing;

namespace Utilities.Logging
{
    public class PathLogger : ILogger
    {
        private StreamWriter _logger;
        private GroundstationCollection _groundstations;

        public PathLogger(string directory,
            GroundstationCollection groundstations)
        {
            // Record information about the attack routes selected for each frame.
            _logger = new StreamWriter(Path.Combine(directory, "paths.csv"));
            _logger.WriteLine("FRAME,PATHS");
            _groundstations = groundstations;
        }

        public void LogEntry(int frameCount, LoggingContext ctx)
        {
            // FRAME
            _logger.Write($"{frameCount}");

            // PATHS
            if (ctx.Routes.Count == 0)
            {
                _logger.Write(",nan");
            }

            foreach (Route route in ctx.Routes)
            {
                _logger.Write($",{_groundstations[route.StartCity]} -> {_groundstations[route.EndCity]}");
            }

            _logger.Write("\n");
            _logger.Flush();
        }
        
        public void Save()
        {
            throw new NotImplementedException();
        }
    }
}