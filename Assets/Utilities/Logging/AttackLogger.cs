using System;
using System.IO;
using Routing;

namespace Utilities.Logging
{

    public class AttackLogger : ILogger
    {

        private readonly StreamWriter _logger;
        private readonly LinkCapacityMonitor _linkCapacityMonitor;

        public AttackLogger(string directory,
            LinkCapacityMonitor linkCapacities)
        {
            _linkCapacityMonitor = linkCapacities;
            _logger = new StreamWriter(Path.Combine(directory, $"attack.csv"));
            _logger.WriteLine("FRAME,TARGET LINK,ROUTE COUNT,FINAL CAPACITY");
        }

        public void LogEntry(int frameCount, LoggingContext ctx)
        // AttackTarget target, List<Route> routes)
        {
            // FRAME
            _logger.Write($"{frameCount},");

            // TARGET LINK
            if (ctx.Target.Link != null && ctx.Target.HasValidTargetLink())
                _logger.Write($"{ctx.Target.Link.SrcNode.Id.ToString()} -> {ctx.Target.Link.DestNode.Id.ToString()},");
            else
                _logger.Write(",");

            // ROUTE COUNT
            _logger.Write($"{ctx.Routes.Count},");

            // FINAL CAPACITY
            if (ctx.Target.Link != null && ctx.Target.HasValidTargetLink())
                _logger.Write(
                    $"{_linkCapacityMonitor.GetCapacity(ctx.Target.Link.SrcNode.Id, ctx.Target.Link.DestNode.Id)}\n");
            else
                _logger.Write("nan\n");

            _logger.Flush();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }
    }
}