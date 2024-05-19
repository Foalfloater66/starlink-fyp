using System.Collections.Generic;
using System.IO;
using Attack;
using Orbits;
using Routing;

namespace Logging
{
    public class PathLogger : ILogger
    {
        private StreamWriter _logger;
        private GroundstationCollection _groundstations;

        public PathLogger(string directory,
            GroundstationCollection groundstations) // TODO: I can split this into their own objects.
        {

            // Record information about the attack routes selected for each frame.
            _logger = new StreamWriter(System.IO.Path.Combine(directory, "paths.csv"));
            _logger.WriteLine("FRAME,PATHS");
            _groundstations = groundstations;
        }

        public void LogEntry(int frameCount, AttackTarget target, List<Route> routes)
        {
            // FRAME
            _logger.Write($"{frameCount}");

            // PATHS
            if (routes.Count == 0)
            {
                _logger.Write(",nan");
            }

            foreach (Route route in routes)
            {
                _logger.Write($",{_groundstations[route.StartCity]} -> {_groundstations[route.EndCity]}");
            }

            _logger.Write("\n");
            _logger.Flush();
        }
    }
}