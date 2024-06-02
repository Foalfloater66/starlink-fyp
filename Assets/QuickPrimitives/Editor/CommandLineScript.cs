using System;
using Attack.Cases;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuickPrimitives.Editor
{
    public class CommandLineScript : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        public static void Run()
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                Main instance = FindObjectOfType<Main>();

                string[] args = System.Environment.GetCommandLineArgs();
                
                // Variable parameters
                instance.caseChoice = (CaseChoice)Enum.Parse(typeof(CaseChoice), args[5]);
                instance.targetLinkDirection = (Direction)Enum.Parse(typeof(Direction), args[6]);
                instance.defenceOn = bool.Parse(args[7]);
                instance.rmax = int.Parse(args[8]);
                instance.runId = int.Parse(args[9]);

                // Fixed parameters
                instance.screenshotMode = false;
                instance.logAttack = true;
                instance.logRTT = true;
                instance.maxFrames = 100;
                
            };
            
            EditorApplication.EnterPlaymode();
            EditorSceneManager.LoadSceneInPlayMode("Assets/Orbits/Scene_SP_basic.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        }
    }
}