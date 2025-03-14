using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class ArcweaveBuildProcessor
{
    private const string DEFAULT_JSON_SOURCE_PATH = "Assets/Arcweave/project.json";
    private const string STREAMING_ASSETS_FOLDER = "Assets/StreamingAssets";
    private const string ARCWEAVE_FOLDER = "arcweave";
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
} 