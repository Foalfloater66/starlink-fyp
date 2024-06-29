using System;
using Attack.Cases;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Automation.Scripts
{
    public static class Single
    {
        public static void Run(string[] args)
        {
            if (args.Length != 15)
            {
                Debug.Log("Incorrect number of arguments provided.");
                Debug.Log(
                    "Usage: -batch <case> <direction> <rmax> <id> <frames> <log_screenshots> <log_attack> <log_rtt> <log_hops>");
                return;
            }

            Runner runner = ScriptableObject.CreateInstance<Runner>();
            Assert.IsNotNull(runner);
            
            runner.Experiments.Enqueue(new Experiment(
                (CaseChoice)Enum.Parse(typeof(CaseChoice), args[6]),
                (Direction)Enum.Parse(typeof(Direction), args[7]),
                int.Parse(args[8]),
                int.Parse(args[9]),
                int.Parse(args[10]),
                bool.Parse(args[11]),
                bool.Parse(args[12]),
                bool.Parse(args[13]),
                bool.Parse(args[14])
            ));

            // Run experiments.
            EditorApplication.EnterPlaymode();
            runner.Next();
        }
    }
}