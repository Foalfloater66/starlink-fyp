using System.Collections.Generic;
using System.IO;
using System.Text;
using Attack;
using Attack.Cases;
using Orbits;
using Routing;
using Path = System.IO.Path;

namespace Utilities
{
    public class Logs
    {
        private StreamWriter _attackLogger;
        private StreamWriter _pathLogger;

        public Logs(string directory, CaseChoice choice, Direction direction) // TODO: I can split this into their own objects.
        {
            // Record information about snapshots.
            _attackLogger = new StreamWriter(Path.Combine(directory, $"{choice}_{direction}.csv"));
            _attackLogger.WriteLine("FRAME,TARGET LINK,ROUTE COUNT,FINAL CAPACITY");
        
            // Record information about the attack routes selected for each frame.
            _pathLogger = new StreamWriter(Path.Combine(directory, "paths.csv"));
            _pathLogger.WriteLine("FRAME,PATHS");
        }

        public void LogRoutesEntry(int frameCount, AttackTarget target, List<Routing.Route> routes, GroundstationCollection groundstations, LinkCapacityMonitor linkCapacityMonitor)
        {
            
            // StringBuilder sb = new StringBuilder()
            _pathLogger.Write($"{frameCount}"); // TODO: I CAN ALSO... not write. AND use a stringbuilder instead. Would be more efficient.
            
            // Target link
            if (target.Link == null || !target.HasValidTargetLink())
            {
                _attackLogger.Write(",,0,nan");
                _pathLogger.Write(",nan");
            }
            
            // Route count + route src-dest pairs
            var sb = new StringBuilder();
            foreach (Routing.Route route in routes)
            {
                sb.Append($",{groundstations[route.StartCity]} -> {groundstations[route.EndCity]}"); // TODO: I can move this to the end.
            }
            _pathLogger.Write(sb.ToString());
            _pathLogger.Write("\n");
            _pathLogger.Flush();

        }

        public void LogAttackEntry(int frameCount, AttackTarget target, List<Routing.Route> routes, GroundstationCollection groundstations, LinkCapacityMonitor linkCapacityMonitor)
        {
            
            // Target link
            if (target.Link != null && target.HasValidTargetLink())
            {
                _attackLogger.Write($",{target.Link.SrcNode.Id.ToString()} -> {target.Link.DestNode.Id.ToString()}");
            }
            else
            {
                _attackLogger.Write(",,0,nan");
            }
            
            // Route count + route src-dest pairs
            _attackLogger.Write($",{routes.Count}");

            // Target link final capacity.
            if (target.Link != null && target.HasValidTargetLink())
            {
                _attackLogger.Write(
                    $",{linkCapacityMonitor.GetCapacity(target.Link.SrcNode.Id, target.Link.DestNode.Id)}");
            }
            
            _attackLogger.Write("\n");
            _attackLogger.Flush();
        }

        // public void LogEntry(int frameCount, AttackTarget target, List<Routing.Path> routes, GroundstationCollection groundstations, LinkCapacityMonitor linkCapacityMonitor)
        // {
        //     // Start.
        //     _attackLogger.Write($"{frameCount}");
        //
        //     // Target link
        //     if (target.Link != null && target.HasValidTargetLink())
        //     {
        //         _attackLogger.Write($",{target.Link.SrcNode.Id.ToString()} -> {target.Link.DestNode.Id.ToString()}");
        //     }
        //     else
        //     {
        //         _attackLogger.Write(",,0,nan");
        //         _pathLogger.Write(",nan");
        //     }
        //     
        //     // Route count + route src-dest pairs
        //     if (routes.Count > 0)
        //     {
        //         var sb = new StringBuilder();
        //         var counter = 0;
        //         foreach (Routing.Path route in routes)
        //         {
        //             sb.Append($",{groundstations[route.StartCity]} -> {groundstations[route.EndCity]}"); // TODO: I can move this to the end.
        //             counter += 1;
        //         }
        //         _attackLogger.Write($",{counter}");
        //         _pathLogger.Write(sb.ToString());
        //     }
        //
        //     // Target link final capacity.
        //     if (target.Link != null && target.HasValidTargetLink())
        //     {
        //         _attackLogger.Write(
        //             $",{linkCapacityMonitor.GetCapacity(target.Link.SrcNode.Id, target.Link.DestNode.Id)}");
        //     }
        //     
        //     _attackLogger.Write("\n");
        //     _pathLogger.Write("\n");
        //     _attackLogger.Flush();
        //     _pathLogger.Flush();
        //     
        // }
        

    }
}