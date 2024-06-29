using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Automation
{
    public class Runner : ScriptableObject
    {
        public bool experimentMode = false;

        public Queue<Experiment> Experiments;

        public void Awake()
        {
            Experiments = new Queue<Experiment>();
        }

        public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            Experiment args = Experiments.Dequeue();
            Main instance = FindObjectOfType<Main>();

            instance.caseChoice = args.choice;
            instance.targetLinkDirection = args.direction;
            instance.defenceOn = args.rMax!= 1;
            instance.rmax = args.rMax;
            instance.runId = args.ID;
            instance.runner = this;
            instance.maxFrames = args.Frames;
            instance.logScreenshots = args.LogScreenshots;
            instance.logAttack = args.LogAttack;
            instance.logRTT = args.LogRTT;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void Next()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorSceneManager.LoadSceneInPlayMode("Assets/Orbits/Scene_SP_basic.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
        }
    }
}