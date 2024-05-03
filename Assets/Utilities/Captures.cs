using System.IO;
using UnityEngine;

namespace Utilities
{
    public static class Captures
    {
        /// <summary>
        /// Set up the directory to save screenshots in.
        /// </summary>
        /// <param name="folderName"></param>
        public static void Setup(string folderName)
        {
            // TODO: move directory out. This should bea n object instead.
            string directory = $"Logs/Captures/{folderName}";
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
            Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Saves a screenshot of the current display on Unity.
        /// </summary>
        public static void CaptureState(string folderName, string fileName)
        {
            string directory = $"Logs/Captures/{folderName}";
            ScreenCapture.CaptureScreenshot($"{directory}/{fileName}.png", 1);
        }
    }
}