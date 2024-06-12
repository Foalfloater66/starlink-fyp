using System.Collections.Generic;
using Attack.Cases;
using Experiments;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Automation.Scripts
{
    [System.Serializable]
    public class ExperimentParameters
    {
        public List<CaseChoice> cases;
        public List<Direction> directions;
        public List<int> rmaxList;
        public int frames; // number of frames to run each run for.
        public int reps; // number of repetitions for randomized runs.
    }

    public static class Batch
    {
        private static ExperimentParameters ReadSpecification(string jsonPath)
        {
            // Parse the JSON file.
            string jsonData = System.IO.File.ReadAllText(jsonPath);
            ExperimentParameters parameters = JsonUtility.FromJson<ExperimentParameters>(jsonData);
            Assert.IsNotNull(parameters);
            return parameters;
        }

        private static void FillExperimentsQueue(Runner runner, ExperimentParameters parameters)
        {
            foreach (CaseChoice choice in parameters.cases)
            {
                foreach (Direction direction in parameters.directions)
                {
                    foreach (int rmax in parameters.rmaxList)
                    {
                        int startID;
                        int reps;
                        if (rmax <= 1)
                        {
                            startID = 0;
                            reps = 1;
                        }
                        else
                        {
                            startID = 1;
                            reps = parameters.reps + 1;
                        }

                        for (int ID = startID; ID < reps; ID++)
                        {
                            runner.Experiments.Enqueue(new Experiment(choice, direction, rmax,
                                parameters.frames, ID, false, true, true));
                        }
                    }
                }
            }
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

            ExperimentParameters parameters = ReadSpecification(args[6]);
            FillExperimentsQueue(runner, parameters);

            // Run experiments.
            EditorApplication.EnterPlaymode();
            runner.Next();
        }
    }
}