using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Automation.Scripts
{
    [System.Serializable]
    public class ExperimentCollection
    {
        public List<Experiment> experiments;
        public int frames;
        public bool logScreenshots;
        public bool logVideo;
        public bool logAttack;
        public bool logRTT;
        public bool logHops;
    }

    public static class Batch
    {
        private static ExperimentCollection ReadExperiments(string jsonPath)
        {
            // Parse the JSON file.
            string jsonData = System.IO.File.ReadAllText(jsonPath);
            ExperimentCollection parameters = JsonUtility.FromJson<ExperimentCollection>(jsonData);
            Assert.IsNotNull(parameters);
            return parameters;
        }

        public static void Run(string[] args)
        {
            if (args.Length != 7)
            {
                Debug.Log("Incorrect number of arguments provided.");
                Debug.Log("Usage: -batch <json_path>");
                return;
            }

            Runner runner = ScriptableObject.CreateInstance<Runner>();
            runner.experimentMode = true;
            Assert.IsNotNull(runner);

            ExperimentCollection specification = ReadExperiments(args[6]);
            
            foreach (Experiment experiment in specification.experiments)
            {
                for (int id = 0; id < experiment.reps; id++)
                {
                    Experiment clone = experiment.Clone();
                    clone.Build(id, specification.frames, specification.logScreenshots, specification.logVideo, specification.logAttack, specification.logRTT, specification.logHops);
                    runner.Experiments.Enqueue(clone);
                }
            }

            // Run experiments.
            EditorApplication.EnterPlaymode();
            runner.Next();
        }
    }
}