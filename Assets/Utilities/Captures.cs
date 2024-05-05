using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Utilities
{
    public  class Captures 
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
            // plus create executable
        }

        /// <summary>
        /// Saves a screenshot of the current display on Unity.
        /// </summary>
        public void CaptureState(string folderName, string fileName, CustomCamera views, Text text)
        {
            string directory = $"Logs/Captures/{folderName}";
            
            Texture2D existingImageTexture = null;
            Texture2D mergedTexture = null;
            
            for (int i = 0; i < views.cam_count; i++)
            {
                Texture2D screenshotTexture = CaptureScreenshotTexture(); //screenshotPath);
                views.SwitchCamera();
                
                if (i == 0)
                {
                    existingImageTexture = screenshotTexture;
                } 
                else {
                    existingImageTexture = ConcatenateTextures(existingImageTexture, screenshotTexture);
                }
            }
            SaveTextureToFile(existingImageTexture, $"{directory}/{fileName}");
        }
        
        static Texture2D  CaptureScreenshotTexture()
        {
            // Set active render texture to the render texture
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
            RenderTexture.active = rt;
            
            // CustomCamera.
            Camera cam = Camera.main;
            Assert.IsNotNull(cam);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            
            // Capture screenshot
            Texture2D screenshotTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            screenshotTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            screenshotTexture.Apply();
            
            cam.targetTexture = null;
            RenderTexture.active = null;
            
            return screenshotTexture;
        }
        
    // Method to merge screenshots with existing image
    static void MergeImages(int screenshotCounter, string finalPath)
    {
        // Load the existing image
        // Texture2D existingImageTexture = LoadTexture(existingImageName);
        // Iterate through captured screenshots
        for (int i = 0; i < screenshotCounter; i++)
        {
            // Load the screenshot


            // Save the merged image
            // string mergedImagePath = mergedImageFolder + "/merged_image" + i + ".png";

            // Clean up
            // Destroy(screenshotTexture);
            // Destroy(mergedTexture);

        }


        // Reset screenshot counter
        screenshotCounter = 0;
    }

    // Load a texture from file
    static Texture2D LoadTexture(string path)
    {
        byte[] fileData = System.IO.File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        return texture;
    }

    // Concatenate two textures horizontally
    static Texture2D ConcatenateTextures(Texture2D texture1, Texture2D texture2)
    {
        // int width = texture1.width + texture2.width;
        int width = texture1.width; 
        int height = texture1.height + texture2.height;
            // Mathf.Max(texture1.height, texture2.height);
        Texture2D result = new Texture2D(width, height);

        // Copy texture1
        result.SetPixels(0, 0, texture1.width, texture1.height, texture1.GetPixels());
        // Copy texture2
        result.SetPixels(0, texture1.height, texture2.width, texture2.height, texture2.GetPixels());

        result.Apply();
        return result;
    }

    // Save a texture to file
    static void SaveTextureToFile(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes($"{path}.png", bytes);
        // while (!TestFileReadability($"{path}.png"))
        // {
        //     
        // }
        // while (!File.Exists($"{path}.png"))
        // {
            
        // }
        // onCompleted?
    }
    
    static bool TestFileReadability(string path)
    {
        try
        {
            using (FileStream stream = File.OpenRead($"{path}.png"))
            {
                return true; // File is readable
            }
        }
        catch (IOException ex)
        {
            Debug.LogError($"Error reading file {path}.png: {ex.Message}");
            return false; // File is not readable
        }
    }
    }
}