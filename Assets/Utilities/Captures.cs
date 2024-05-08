﻿using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Utilities
{
    public  class Captures
    {
        private readonly string _imgPathFormat;

        /// <summary>
        /// Set up the directory to save screenshots in.
        /// </summary>
        /// <param name="outDirectory"></param>
        /// <param name="imgNameFormat"></param>
        public Captures(string outDirectory, string imgNameFormat)
        {
            _imgPathFormat = Path.Combine(outDirectory, imgNameFormat);
        }

        /// <summary>
        /// Saves a screenshot of the current display on Unity.
        /// </summary>
        public void CaptureState(CustomCamera views, Text text, int imgId)
        {
            Canvas.ForceUpdateCanvases();
            text.canvas.renderMode = RenderMode.ScreenSpaceCamera;
            text.canvas.worldCamera = Camera.main;
            
            // string directory = $"Logs/Captures/{folderName}";
            Texture2D existingImageTexture = null;
            
            for (int i = 0; i < views.cam_count; i++)
            {
                Texture2D screenshotTexture = CaptureScreenshotTexture();
                views.SwitchCamera();
                
                if (i == 0)
                {
                    existingImageTexture = screenshotTexture;
                } 
                else {
                    existingImageTexture = ConcatenateTextures(existingImageTexture, screenshotTexture);
                }
            }

            SaveTextureToFile(existingImageTexture, $"{_imgPathFormat}_{imgId:00}");
        }
        
        static Texture2D  CaptureScreenshotTexture()
        {
            // Set up the render texture and assign it to the current camera.
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
            RenderTexture.active = rt;
            Camera cam = Camera.main;
            Assert.IsNotNull(cam);
            cam.targetTexture = rt; 
            // Thread.Sleep(3000);
            cam.Render();

            
            // Capture screenshot
            Texture2D screenshotTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
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
    static Texture2D ConcatenateTextures(Texture2D texture1, Texture2D texture2)
    {
        int width = texture1.width; 
        int height = texture1.height + texture2.height;
        Texture2D result = new Texture2D(width, height);

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
    static void SaveTextureToFile(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes($"{path}.png", bytes);
    }
    
    }
}