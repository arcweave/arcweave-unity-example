using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class ArcweaveBuildProcessor
{
    private const string ARCWEAVE_FOLDER = "arcweave";
    private const string IMAGES_FOLDER = "resources";

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
        
        Debug.Log($"Arcweave: Using the bundled project. Optional external content folder: {arcweaveFolderPath}");
    }
}
