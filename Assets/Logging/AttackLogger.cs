using System.Collections.Generic;
using System.IO;
using Attack;
using Attack.Cases;
using Routing;

namespace Logging
{

    public class AttackLogger : ILogger
    {

        private readonly StreamWriter _logger;
        private readonly LinkCapacityMonitor _linkCapacityMonitor;

        public AttackLogger(string directory, CaseChoice caseChoice, Direction targetLinkDirection,
            LinkCapacityMonitor linkCapacities)
        {
            _linkCapacityMonitor = linkCapacities;
            _logger = new StreamWriter(System.IO.Path.Combine(directory, $"{caseChoice}_{targetLinkDirection}.csv"));
            _logger.WriteLine("FRAME,TARGET LINK,ROUTE COUNT,FINAL CAPACITY");
        }

        public void LogEntry(int frameCount, AttackTarget target, List<Route> routes)
        {
            // FRAME
            _logger.Write($"{frameCount},");

            // TARGET LINK
            if (target.Link != null && target.HasValidTargetLink())
                _logger.Write($"{target.Link.SrcNode.Id.ToString()} -> {target.Link.DestNode.Id.ToString()},");
            else
                _logger.Write(",");

            // ROUTE COUNT
            _logger.Write($"{routes.Count},");

            // FINAL CAPACITY
            if (target.Link != null && target.HasValidTargetLink())
                _logger.Write(
                    $"{_linkCapacityMonitor.GetCapacity(target.Link.SrcNode.Id, target.Link.DestNode.Id)}\n");
            else
                _logger.Write("nan\n");

            _logger.Flush();
        }
    }
}