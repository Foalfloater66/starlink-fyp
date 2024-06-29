using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Utilities.Logging
{

    [System.Serializable]
    public class Hop
    {
        public int frame;
        public List<int> hop;
    }

    [System.Serializable]
    public class HopList
    {
        public List<Hop> latencies;
        public HopList()
        {
            latencies = new List<Hop>();
        }
    }
    public class HopLogger : ILogger
    {
        public readonly HopList _hops;
        private readonly string outputPath;

        public HopLogger(string directory)
        {
            outputPath = Path.Combine(directory, "hops.json");
            _hops = new HopList();
        }

        public void LogEntry(int frameCount, LoggingContext ctx)
        {
            // FRAME + LATENCIES
            Hop hop = new Hop();
            hop.frame = frameCount;
            hop.hop = ctx.Hops;
            _hops.latencies.Add(hop);
        }

        public void Save()
        {
            string jsonString = JsonUtility.ToJson(_hops, true);
            File.WriteAllText(outputPath, jsonString);

        }
    }
}