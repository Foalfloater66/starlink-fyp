using System.Collections.Generic;
using Attack.Cases;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuickPrimitives.Editor
{
    [System.Serializable]
    public class ExperimentsSpecification
    {
        public List<CaseChoice> cases;
        public List<Direction> directions;
        public List<int> rmaxList;
        public int frames; // number of frames to run each run for.
        public int reps; // number of repetitions for randomized runs.
    }

    public struct RunArgs
    {        public CaseChoice Choice;
        public Direction Direction ;
        public int Rmax ;
        public int frames;
        public int id ;
    public RunArgs
        (CaseChoice choice, Direction direction, int rmax, int frames, int id)
    {
        Choice = choice;
        Direction = direction;
        Rmax = rmax;
        this.frames = frames;
        this.id = id;
    }
        
    }

    // public class ActualExperiments 
    // {
    //     private List<RunArgs> run_names;
    //     
    //     public ActualExperiments(List<RunArgs> run_args)
    //     {
    //         run_names = run_args;
    //     }
    //
    //     public Awake()
    //     {
    //         
    //     }
    // }
    public class Experiments : ScriptableObject
    {

        // public TextAsset _jsonFile;
        
        public Queue<RunArgs> run_names;
        public int frames;

        public void Awake()
        {
            run_names = new Queue<RunArgs>();
            // throw new NotImplementedException();
        }



        public static void SetParams(Main instance, CaseChoice choice, Direction direction, bool defenceOn, int rmax, int runId)
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                // run_names.Dequeue()
                // Main instance = FindObjectOfType<Main>();

                // string[] args = System.Environment.GetCommandLineArgs();

                // Variable parameters
                // instance.caseChoice = (CaseChoice)Enum.Parse(typeof(CaseChoice), args[5]);
                // instance.targetLinkDirection = (Direction)Enum.Parse(typeof(Direction), args[6]);
                // instance.defenceOn = bool.Parse(args[7]);
                // instance.rmax = int.Parse(args[8]);
                // instance.runId = int.Parse(args[9]);
                instance.caseChoice = choice; //(CaseChoice)Enum.Parse(typeof(CaseChoice), args[5]);
                instance.targetLinkDirection = direction; //(Direction)Enum.Parse(typeof(Direction), args[6]);
                instance.defenceOn = defenceOn; //bool.Parse(args[7]);
                instance.rmax = rmax; //int.Parse(args[8]);
                instance.runId = runId; //int.Parse(args[9]);

                // Fixed parameters
                instance.screenshotMode = false;
                instance.logAttack = true;
                instance.logRTT = true;
                instance.maxFrames = 10;

            };

            
            // EditorSceneManager.LoadSceneInPlayMode("Assets/Orbits/Scene_SP_basic.unity",
            //     new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        }

        public void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            Debug.Log("I'm been called.");
            RunArgs args = run_names.Dequeue();
                
                
            Main instance = FindObjectOfType<Main>();

            // string[] args = System.Environment.GetCommandLineArgs();

            // Variable parameters
            // instance.caseChoice = (CaseChoice)Enum.Parse(typeof(CaseChoice), args[5]);
            // instance.targetLinkDirection = (Direction)Enum.Parse(typeof(Direction), args[6]);
            // instance.defenceOn = bool.Parse(args[7]);
            // instance.rmax = int.Parse(args[8]);
            // instance.runId = int.Parse(args[9]);
            instance.caseChoice = args.Choice; //(CaseChoice)Enum.Parse(typeof(CaseChoice), args[5]);
            instance.targetLinkDirection = args.Direction; //(Direction)Enum.Parse(typeof(Direction), args[6]);
            instance.defenceOn = args.Rmax != 1; //bool.Parse(args[7]);
            instance.rmax = args.Rmax; //int.Parse(args[8]);
            instance.runId = args.id; //int.Parse(args[9]);
            instance.exp = this;

            // Fixed parameters
            instance.screenshotMode = false;
            instance.logAttack = true;
            instance.logRTT = true;

            instance.maxFrames = args.frames;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        public void Next()
        {
            // TODO: add a condition if the queue is empty.
            Debug.Log(run_names.Count);
            Debug.Log("No peeking!");
            if (run_names.Count == 0)
            {
                Debug.Log("Run completed.");
                EditorApplication.isPaused = true;
                EditorApplication.Exit(0);
            }
            Debug.Log("Continuing.");
            
            
            // EditorApplication.EnterPlaymode();

            SceneManager.sceneLoaded += OnSceneLoaded;
            // (scene, mode) =>
            // {
            //     
            // };
            EditorSceneManager.LoadSceneInPlayMode("Assets/Orbits/Scene_SP_basic.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            
        }
    }
}