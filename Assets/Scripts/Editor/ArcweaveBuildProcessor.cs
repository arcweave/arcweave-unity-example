using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections.Generic;

public class ArcweaveBuildProcessor
{
    private const string DEFAULT_IMAGES_SOURCE_FOLDER = "Assets/Resources";
    private const string ARCWEAVE_FOLDER = "arcweave";
    private const string IMAGES_FOLDER = "resources";
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
        
        // The scene already contains the imported project. Do not replace it with a stale export.
        // Users can deliberately place a newer project.json in this folder after building.
        
        // Prompt user about the changes
        Debug.Log("Arcweave: StreamingAssets removed from the build process.");
        Debug.Log($"Arcweave: Project folders created at: {arcweaveFolderPath}");
        Debug.Log("Arcweave: Please place your project.json and images in this folder after build.");
    }
} 
