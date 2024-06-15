using System.Collections.Generic;
using Attack.Cases;
using Experiments;
using UnityEditor;
using UnityEditor.CrashReporting;
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
        public bool logAttack;
        public bool logRTT;
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
                    experiment.Build(id, specification.frames, specification.logScreenshots, specification.logAttack, specification.logRTT);
                    runner.Experiments.Enqueue(experiment);
                }
            }

            // Run experiments.
            EditorApplication.EnterPlaymode();
            runner.Next();
        }
    }
}