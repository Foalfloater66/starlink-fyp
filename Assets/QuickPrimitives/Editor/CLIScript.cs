using Attack.Cases;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace QuickPrimitives.Editor
{
    public class CLIScript : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        public static void Run()
        {
            // Parse the JSON file.

            Experiments expObject = ScriptableObject.CreateInstance<Experiments>();
            Assert.IsNotNull(expObject);
            string jsonPath = "./ExperimentsSpecification.json";
            string jsonData = System.IO.File.ReadAllText(jsonPath);
            ExperimentsSpecification experiments = JsonUtility.FromJson<ExperimentsSpecification>(jsonData);
            Assert.IsNotNull(experiments);
            foreach (CaseChoice choice in experiments.cases)
            {
                foreach (Direction direction in experiments.directions)
                {
                    foreach (int rmax in experiments.rmaxList)
                    {
                        if (rmax > 1)
                        {
                            for (int i = 1;
                                 i < experiments.reps + 1; i ++ )
                            {
                                expObject.run_names.Enqueue(new RunArgs(choice, direction,  rmax, experiments.frames,i));
                            }
                        }
                        else
                        {
                            expObject.run_names.Enqueue(new RunArgs(choice, direction, rmax, experiments.frames,0));
                        }
                    }
                }
            }
            
            // Run the experiments for the parameters in the JSON file.
            EditorApplication.EnterPlaymode();
            expObject.Next();


        }
    }
}