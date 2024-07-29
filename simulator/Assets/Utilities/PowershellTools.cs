using System.Diagnostics;
using Attack.Cases;
using UnityEngine;


namespace Utilities
{
    public static class PowershellTools
    {
        public static void SaveVideo(string loggingDirectory, int camId)
        {
            var imgHeight = Screen.height; // * cam.cam_count;
            var imgWidth = Screen.width;
            var command =
                $"ffmpeg -framerate 3 -i {loggingDirectory}/frame_%02d_cam{camId}.png -vf \"scale={imgWidth}:{imgHeight}\" -c:v libx265 -preset fast -crf 20 -pix_fmt yuv420p {loggingDirectory}/output{camId}.mp4";
            PowershellTools.ExecutePowershellCommand(command);
        }

        private static void ExecutePowershellCommand(string command)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process() { StartInfo = startInfo };

            process.Start();
            var errors = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(5000)) // Kills the process after 5 seconds.
                process.Kill();
            process.Close();

            if (!string.IsNullOrEmpty(errors)) UnityEngine.Debug.LogError("PowerShell Errors: " + errors);
        }
    }
}