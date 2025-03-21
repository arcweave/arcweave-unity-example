using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections.Generic;

public class ArcweaveBuildProcessor
{
    private const string DEFAULT_JSON_SOURCE_PATH = "Assets/Arcweave/project.json";
    private const string DEFAULT_IMAGES_SOURCE_FOLDER = "Assets/Resources";
    private const string STREAMING_ASSETS_FOLDER = "Assets/StreamingAssets";
    private const string ARCWEAVE_FOLDER = "arcweave";
    private const string IMAGES_FOLDER = "images";
    private const string JSON_FILENAME = "project.json";

    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        Debug.Log($"Build completed at: {pathToBuiltProject}");
        
        // Create arcweave folder in the build directory
        string buildDirectory = Path.GetDirectoryName(pathToBuiltProject);
        string arcweaveFolderPath = Path.Combine(buildDirectory, ARCWEAVE_FOLDER);
        
        if (!Directory.Exists(arcweaveFolderPath))
        {
            Directory.CreateDirectory(arcweaveFolderPath);
            Debug.Log($"Created arcweave folder at: {arcweaveFolderPath}");
        }
        
        // Create images folder in the build directory
        string imagesFolderPath = Path.Combine(arcweaveFolderPath, IMAGES_FOLDER);
        if (!Directory.Exists(imagesFolderPath))
        {
            Directory.CreateDirectory(imagesFolderPath);
            Debug.Log($"Created images folder at: {imagesFolderPath}");
        }
    }

    [MenuItem("Arcweave/Copy Project to StreamingAssets")]
    public static void CopyProjectToStreamingAssets()
    {
        CopyJsonToStreamingAssets();
        CopyImagesToStreamingAssets();
    }

    [MenuItem("Arcweave/Copy JSON to StreamingAssets")]
    public static void CopyJsonToStreamingAssets()
    {
        // Ensure StreamingAssets folder exists
        if (!Directory.Exists(STREAMING_ASSETS_FOLDER))
        {
            Directory.CreateDirectory(STREAMING_ASSETS_FOLDER);
        }
        
        // Ensure arcweave folder exists in StreamingAssets
        string arcweaveFolderPath = Path.Combine(STREAMING_ASSETS_FOLDER, ARCWEAVE_FOLDER);
        if (!Directory.Exists(arcweaveFolderPath))
        {
            Directory.CreateDirectory(arcweaveFolderPath);
        }
        
        // Source JSON path
        string sourceJsonPath = DEFAULT_JSON_SOURCE_PATH;
        
        // Check if source JSON exists
        if (!File.Exists(sourceJsonPath))
        {
            Debug.LogError($"Source JSON file not found at: {sourceJsonPath}");
            return;
        }
        
        // Destination JSON path
        string destJsonPath = Path.Combine(arcweaveFolderPath, JSON_FILENAME);
        
        // Copy the file
        File.Copy(sourceJsonPath, destJsonPath, true);
        Debug.Log($"Copied JSON file from {sourceJsonPath} to {destJsonPath}");
        
        // Refresh AssetDatabase to show the new file
        AssetDatabase.Refresh();
    }
    
    [MenuItem("Arcweave/Copy Images to StreamingAssets")]
    public static void CopyImagesToStreamingAssets()
    {
        // Ensure StreamingAssets folder exists
        if (!Directory.Exists(STREAMING_ASSETS_FOLDER))
        {
            Directory.CreateDirectory(STREAMING_ASSETS_FOLDER);
        }
        
        // Ensure arcweave folder exists in StreamingAssets
        string arcweaveFolderPath = Path.Combine(STREAMING_ASSETS_FOLDER, ARCWEAVE_FOLDER);
        if (!Directory.Exists(arcweaveFolderPath))
        {
            Directory.CreateDirectory(arcweaveFolderPath);
        }
        
        // Ensure images folder exists in StreamingAssets/arcweave
        string imagesDestFolder = Path.Combine(arcweaveFolderPath, IMAGES_FOLDER);
        if (!Directory.Exists(imagesDestFolder))
        {
            Directory.CreateDirectory(imagesDestFolder);
        }
        
        // Source images folder
        string imagesSourceFolder = DEFAULT_IMAGES_SOURCE_FOLDER;
        
        // Check if source folder exists
        if (!Directory.Exists(imagesSourceFolder))
        {
            Debug.LogWarning($"Source images folder not found at: {imagesSourceFolder}. No images will be copied.");
            return;
        }
        
        // Get all image files
        string[] imageExtensions = { "*.png", "*.jpg", "*.jpeg", "*.gif" };
        List<string> imageFiles = new List<string>();
        
        foreach (string extension in imageExtensions)
        {
            string[] files = Directory.GetFiles(imagesSourceFolder, extension);
            imageFiles.AddRange(files);
        }
        
        if (imageFiles.Count == 0)
        {
            Debug.LogWarning($"No image files found in {imagesSourceFolder}");
            return;
        }
        
        // Copy each image file
        int copiedCount = 0;
        foreach (string sourceImagePath in imageFiles)
        {
            string fileName = Path.GetFileName(sourceImagePath);
            string destImagePath = Path.Combine(imagesDestFolder, fileName);
            
            File.Copy(sourceImagePath, destImagePath, true);
            copiedCount++;
        }
        
        Debug.Log($"Copied {copiedCount} image files to {imagesDestFolder}");
        
        // Refresh AssetDatabase to show the new files
        AssetDatabase.Refresh();
    }
} 