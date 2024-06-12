using System.Collections.Generic;
using Experiments;
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

            instance.caseChoice = args.Choice;
            instance.targetLinkDirection = args.Direction;
            instance.defenceOn = args.Rmax != 1;
            instance.rmax = args.Rmax;
            instance.runId = args.ID;
            instance.runner = this;
            instance.maxFrames = args.Frames;
            instance.screenshotMode = args.LogScreenshots;
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