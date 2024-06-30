using System.Diagnostics;
using Attack.Cases;
using UnityEngine;


namespace Utilities
{
    public static class PowershellTools
    {
        public static void SaveVideo(CustomCamera cam, string loggingDirectory, CaseChoice caseChoice, Direction targetLinkDirection, bool defenceOn, int rmax)
        {
            var imgHeight = Screen.height * cam.cam_count;
            var imgWidth = Screen.width;
            
            string type;
            if (!defenceOn)
            {
                type = "OFF";
            }
            else
            {
                type = rmax.ToString();
            }

            var command =
                $"ffmpeg -framerate 3 -i {loggingDirectory}/frame_%02d.png -vf \"scale={imgWidth}:{imgHeight}\" -c:v libx265 -preset fast -crf 20 -pix_fmt yuv420p {loggingDirectory}/output.mp4";
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