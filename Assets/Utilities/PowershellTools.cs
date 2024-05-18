using System.Diagnostics;
using Attack.Cases;


namespace Utilities
{
    public static class PowershellTools
    {
        public static void SaveVideo(CustomCamera cam, string _loggingDirectory, CaseChoice caseChoice, Direction targetLinkDirection)
        {
            var imgHeight = 748 * cam.cam_count;
            var imgWidth = 1504;

            var command =
                // $"ffmpeg -framerate 3 -i {Directory.GetCurrentDirectory()}/Logs/Captures/{_loggingDirectory}/{qualitativeCase}_{targetLinkDirection}_%02d.png -vf \"scale={imgWidth}:{imgHeight}\" -c:v libx265 -preset fast -crf 20 -pix_fmt yuv420p {Directory.GetCurrentDirectory()}/Logs/Captures/{_loggingDirectory}/output.mp4";
                $"ffmpeg -framerate 3 -i {_loggingDirectory}/{caseChoice}_{targetLinkDirection}_%02d.png -vf \"scale={imgWidth}:{imgHeight}\" -c:v libx265 -preset fast -crf 20 -pix_fmt yuv420p {_loggingDirectory}/output.mp4";
            ExecutePowershellCommand(command);
        }

        public static void PlotData(CustomCamera cam, string _loggingDirectory, CaseChoice caseChoice, Direction targetLinkDirection)
        {
            var command =
                $"python generate_graph.py {_loggingDirectory}/{caseChoice}_{targetLinkDirection}.csv {_loggingDirectory}/{caseChoice}_{targetLinkDirection}_graph.svg";
            ExecutePowershellCommand(command);
        }

        private static void ExecutePowershellCommand(string command)
        {
            // TODO: move this stuff somewhere else :)
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