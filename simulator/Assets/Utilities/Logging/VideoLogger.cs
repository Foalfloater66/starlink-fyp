/* C138 Final Year Project 2023-2024 */

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Utilities.Logging
{
    public class VideoLogger
    {
        private readonly string _imgPathFormat;
        private readonly string _loggingDirectory;
        private List<string> _screenshotPaths = new List<string>();

        /// <summary>
        /// Set up the directory to save screenshots in.
        /// </summary>
        /// <param name="outDirectory"></param>
        public VideoLogger(string outDirectory)
        {
            _loggingDirectory = outDirectory;
            _imgPathFormat = Path.Combine(outDirectory, "frame");
        }

        /// <summary>
        /// Saves a screenshot of the current display on Unity.
        /// </summary>
        public void RecordFrame(CustomCamera views, Text text, int imgId)
        {
            Canvas.ForceUpdateCanvases();
            text.canvas.renderMode = RenderMode.ScreenSpaceCamera;
            text.canvas.worldCamera = Camera.main;

            for (var i = 0; i < views.cam_count; i++)
            {
                Texture2D screenshotTexture = CaptureScreenshotTexture();
                SaveTextureToFile(screenshotTexture, $"{_imgPathFormat}_{imgId:00}_cam{i}");
                views.SwitchCamera();
            }
        }

        private static Texture2D CaptureScreenshotTexture()
        {
            // Set up the render texture and assign it to the current camera.
            var rt = new RenderTexture(Screen.width, Screen.height, 24);
            RenderTexture.active = rt;
            var cam = Camera.main;
            Assert.IsNotNull(cam);
            cam.targetTexture = rt;
            // Thread.Sleep(3000);
            cam.Render();


            // Capture screenshot
            var screenshotTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            screenshotTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            screenshotTexture.Apply();

            // Cleanup
            cam.targetTexture = null;
            RenderTexture.active = null;

            return screenshotTexture;
        }

        /// <summary>
        /// Save a texture to a file.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="path"></param>
        private void SaveTextureToFile(Texture2D texture, string path)
        {
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes($"{path}.png", bytes);
            this._screenshotPaths.Add($"{path}.png");
        }

        /// <summary>
        /// Saves the video to the logging directory.
        /// </summary>
        public void Save(CustomCamera cam)
        {
            for (int i = 0; i < cam.cam_count; i++)
            {
                PowershellTools.SaveVideo(_loggingDirectory, i);
            }
        }

        /// <summary>
        /// Deletes all images used to create the video.
        /// </summary>
        public void Clean()
        {
            foreach (string file in _screenshotPaths)
            {
                File.Delete(file);
            }
        }
    }
}