using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections.Generic;

public class ArcweaveBuildProcessor
{
    private const string DEFAULT_JSON_SOURCE_PATH = "Assets/Arcweave/project.json";
    private const string DEFAULT_IMAGES_SOURCE_FOLDER = "Assets/Resources";
    private const string ARCWEAVE_FOLDER = "arcweave";
    private const string IMAGES_FOLDER = "images";
    private const string JSON_FILENAME = "project.json";

    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        Debug.Log($"Arcweave: Build completed at: {pathToBuiltProject}");
        
        // Create folders in the build directory for user-added content only
        CreateFoldersInBuildDirectory(pathToBuiltProject);
    }
    
    /// <summary>
    /// Creates folders in the build directory for user-added content
    /// </summary>
    private static void CreateFoldersInBuildDirectory(string pathToBuiltProject)
    {
        // Create arcweave folder in the build directory
        string buildDirectory = Path.GetDirectoryName(pathToBuiltProject);
        string arcweaveFolderPath = Path.Combine(buildDirectory, ARCWEAVE_FOLDER);
        
        if (!Directory.Exists(arcweaveFolderPath))
        {
            try {
                Directory.CreateDirectory(arcweaveFolderPath);
                Debug.Log($"Arcweave: Created arcweave folder at: {arcweaveFolderPath}");
            } catch (System.Exception e) {
                Debug.LogError($"Arcweave: Failed to create arcweave folder: {e.Message}");
            }
        }
        
        // Create images folder in the build directory
        string imagesFolderPath = Path.Combine(arcweaveFolderPath, IMAGES_FOLDER);
        if (!Directory.Exists(imagesFolderPath))
        {
            try {
                Directory.CreateDirectory(imagesFolderPath);
                Debug.Log($"Arcweave: Created images folder at: {imagesFolderPath}");
            } catch (System.Exception e) {
                Debug.LogError($"Arcweave: Failed to create images folder: {e.Message}");
            }
        }
        
        // Create a copy of the JSON in the build directory (optional)
        string sourceJsonPath = DEFAULT_JSON_SOURCE_PATH;
        if (File.Exists(sourceJsonPath))
        {
            try {
                string destJsonPath = Path.Combine(arcweaveFolderPath, JSON_FILENAME);
                File.Copy(sourceJsonPath, destJsonPath, true);
                Debug.Log($"Arcweave: Copied JSON file to build directory: {destJsonPath}");
            } catch (System.Exception e) {
                Debug.LogWarning($"Arcweave: Failed to copy JSON to build directory: {e.Message}");
            }
        }
        
        // Prompt user about the changes
        Debug.Log("Arcweave: StreamingAssets removed from the build process.");
        Debug.Log($"Arcweave: Project folders created at: {arcweaveFolderPath}");
        Debug.Log("Arcweave: Please place your project.json and images in this folder after build.");
    }
} 