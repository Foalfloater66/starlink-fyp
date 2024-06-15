using Automation.Scripts;
using UnityEngine;
using Single = Automation.Scripts.Single;

namespace QuickPrimitives.Editor
{
    public class CLI : MonoBehaviour
    {
        
        [RuntimeInitializeOnLoadMethod]
        public static void Run()
        {
            string[] args = System.Environment.GetCommandLineArgs();

            if (args.Length < 6) return;

            string cmd = args[5];
            switch (cmd)
            {
                case "-batch":
                    Batch.Run(args);
                    break;
                case "-single":
                    Single.Run(args);
                    break;
            }
        }
    }
}