/* C138 Final Year Project 2023-2024 */

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Utilities.Logging
{
    [System.Serializable]
    public class Latency
    {
        public int frame;
        public List<float> rtt;
    }

    [System.Serializable]
    public class LatencyList
    {
        public List<Latency> latencies;

        public LatencyList()
        {
            latencies = new List<Latency>();
        }
    }

    public class LatencyLogger : ILogger
    {
        public readonly LatencyList _latencies;
        private readonly string outputPath;

        public LatencyLogger(string directory)
        {
            outputPath = Path.Combine(directory, "rtt.json");
            _latencies = new LatencyList();
        }

        public void LogEntry(int frameCount, LoggingContext ctx)
        {
            // FRAME + LATENCIES
            Latency latency = new Latency();
            latency.frame = frameCount;
            latency.rtt = ctx.RTT;
            _latencies.latencies.Add(latency);
        }

        public void Save()
        {
            string jsonString = JsonUtility.ToJson(_latencies, true);
            File.WriteAllText(outputPath, jsonString);
        }
    }
}