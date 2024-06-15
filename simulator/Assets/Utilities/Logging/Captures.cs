using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Utilities;

namespace Logging
{
    public class Captures
    {
        private readonly string _imgPathFormat;

        /// <summary>
        /// Set up the directory to save screenshots in.
        /// </summary>
        /// <param name="outDirectory"></param>
        public Captures(string outDirectory)
        {
            _imgPathFormat = Path.Combine(outDirectory, "frame");
        }

        /// <summary>
        /// Saves a screenshot of the current display on Unity.
        /// </summary>
        public void TakeScreenshot(CustomCamera views, Text text, int imgId)
        {
            Canvas.ForceUpdateCanvases();
            text.canvas.renderMode = RenderMode.ScreenSpaceCamera;
            text.canvas.worldCamera = Camera.main;

            // string directory = $"Logs/Captures/{folderName}";
            Texture2D existingImageTexture = null;

            for (var i = 0; i < views.cam_count; i++)
            {
                var screenshotTexture = CaptureScreenshotTexture();
                views.SwitchCamera();

                if (i == 0)
                    existingImageTexture = screenshotTexture;
                else
                    existingImageTexture = ConcatenateTextures(existingImageTexture, screenshotTexture);
            }

            SaveTextureToFile(existingImageTexture, $"{_imgPathFormat}_{imgId:00}");
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
        /// Concatenate two textures vertically.
        /// </summary>
        /// <param name="texture1">Top 2D texture</param>
        /// <param name="texture2">Bottom 2D texture</param>
        /// <returns></returns>
        private static Texture2D ConcatenateTextures(Texture2D texture1, Texture2D texture2)
        {
            var width = texture1.width;
            var height = texture1.height + texture2.height;
            var result = new Texture2D(width, height);

            // Copy both textures.
            result.SetPixels(0, 0, texture1.width, texture1.height, texture1.GetPixels());
            result.SetPixels(0, texture1.height, texture2.width, texture2.height, texture2.GetPixels());
            result.Apply();
            return result;
        }

        /// <summary>
        /// Save a texture to a file.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="path"></param>
        private static void SaveTextureToFile(Texture2D texture, string path)
        {
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes($"{path}.png", bytes);
        }
    }
}