using UnityEngine;
using Arcweave;
using UnityEngine.Events;
using System.IO;

public class RuntimeArcweaveImporter : MonoBehaviour
{
    public ArcweaveProjectAsset arcweaveAsset;
    
    // Events to notify import status
    public UnityEvent onImportStarted;
    public UnityEvent onImportSuccess;
    public UnityEvent onImportFailed;

    // Web import credentials
    public string apiKey;
    public string projectHash;
    
    // Local JSON file path (relative to build folder)
    public string localJsonFilePath = "arcweave/project.json";

    [Header("Particle System Control")]
    public ParticleSystemController particleSystemController;

    private bool isImporting = false;
    private bool hasLoadedPrepackagedJson = false;

    private void Start()
    {
        // Copy initial values from asset
        if (arcweaveAsset != null)
        {
            apiKey = arcweaveAsset.userAPIKey;
            projectHash = arcweaveAsset.projectHash;
        }
        
        // Try to load prepackaged JSON from StreamingAssets
        LoadPrepackagedJson();
    }
    
    // Load the prepackaged JSON file from StreamingAssets
    private void LoadPrepackagedJson()
    {
        if (arcweaveAsset == null || hasLoadedPrepackagedJson)
            return;
            
        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, localJsonFilePath);
        
        if (File.Exists(streamingAssetsPath))
        {
            Debug.Log($"Loading prepackaged JSON from StreamingAssets: {streamingAssetsPath}");
            try
            {
                string jsonContent = File.ReadAllText(streamingAssetsPath);
                
                // Set asset for JSON import
                arcweaveAsset.importSource = ArcweaveProjectAsset.ImportSource.FromJson;
                
                // Import project from JSON
                arcweaveAsset.MakeProjectFromJson(jsonContent, () => {
                    if (arcweaveAsset.Project != null)
                    {
                        Debug.Log("Prepackaged project loaded successfully!");
                        hasLoadedPrepackagedJson = true;
                        UpdateParticleSystem();
                    }
                    else
                    {
                        Debug.LogError("Failed to load prepackaged project!");
                    }
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error reading prepackaged JSON file: {e.Message}");
            }
        }
        else
        {
            Debug.Log($"No prepackaged JSON found at: {streamingAssetsPath}");
        }
    }

    // Import from web
    public void ImportFromWeb()
    {
        if (isImporting || arcweaveAsset == null)
        {
            Debug.LogWarning("Already importing or asset not assigned");
            return;
        }

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(projectHash))
        {
            Debug.LogError("API Key and Project Hash must be set for web import!");
            onImportFailed?.Invoke();
            return;
        }

        isImporting = true;
        onImportStarted?.Invoke();

        // Set asset values for web import
        arcweaveAsset.importSource = ArcweaveProjectAsset.ImportSource.FromWeb;
        arcweaveAsset.userAPIKey = apiKey;
        arcweaveAsset.projectHash = projectHash;

        // Import project
        arcweaveAsset.ImportProject(() => {
            isImporting = false;
            if (arcweaveAsset.Project != null)
            {
                Debug.Log("Project imported successfully from web!");
                onImportSuccess?.Invoke();
                UpdateParticleSystem();
                UpdateGameState();
            }
            else
            {
                Debug.LogError("Failed to import project from web!");
                onImportFailed?.Invoke();
            }
        });
    }
    
    // Import from local file
    public void ImportFromLocalFile()
    {
        if (isImporting || arcweaveAsset == null)
        {
            Debug.LogWarning("Already importing or asset not assigned");
            return;
        }

        isImporting = true;
        onImportStarted?.Invoke();
        
        // Get the full path to the JSON file
        string fullPath = GetLocalFilePath();
        
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Local JSON file not found at: {fullPath}");
            isImporting = false;
            onImportFailed?.Invoke();
            return;
        }
        
        try
        {
            string jsonContent = File.ReadAllText(fullPath);
            
            // Set asset for JSON import
            arcweaveAsset.importSource = ArcweaveProjectAsset.ImportSource.FromJson;
            
            // Import project from JSON
            arcweaveAsset.MakeProjectFromJson(jsonContent, () => {
                isImporting = false;
                if (arcweaveAsset.Project != null)
                {
                    Debug.Log("Project imported successfully from local file!");
                    onImportSuccess?.Invoke();
                    UpdateParticleSystem();
                    UpdateGameState();
                }
                else
                {
                    Debug.LogError("Failed to import project from local file!");
                    onImportFailed?.Invoke();
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading local JSON file: {e.Message}");
            isImporting = false;
            onImportFailed?.Invoke();
        }
    }

    // Get the full path to the local JSON file
    private string GetLocalFilePath()
    {
        // If the path is already absolute, use it directly
        if (Path.IsPathRooted(localJsonFilePath))
        {
            return localJsonFilePath;
        }
        
        // Otherwise, combine with the build folder path
        string basePath = Application.isEditor ? 
            Application.dataPath.Replace("/Assets", "") : // Use project root in editor
            Path.GetDirectoryName(Application.dataPath);  // Use build folder in build
            
        return Path.Combine(basePath, localJsonFilePath);
    }

    // Display the path where users should place their JSON files
    public string GetUserFriendlyPath()
    {
        if (Path.IsPathRooted(localJsonFilePath))
        {
            return localJsonFilePath;
        }
        
        string basePath = Application.isEditor ? 
            "[Project Root]" : 
            "[Game Folder]";
            
        return $"{basePath}/{localJsonFilePath.Replace('\\', '/')}";
    }

    private void UpdateParticleSystem()
    {
        if (particleSystemController != null)
        {
            particleSystemController.UpdateParticleSystem();
        }
    }
    
    private void UpdateGameState()
    {
        // Find and reinitialize ArcweavePlayer
        var player = FindAnyObjectByType<ArcweavePlayer>();
        if (player != null)
        {
            player.ResetVariables();
            player.aw = arcweaveAsset;
        }
        
        // Update variables
        var variableEvents = FindAnyObjectByType<ArcweaveVariableEvents>();
        if (variableEvents != null)
        {
            variableEvents.ResetObjectActivation();
            variableEvents.UpdateSliderColor();
            variableEvents.UpdateHealthFromVariable();
            variableEvents.UpdateObjectActivation();
        }
        
        // Resume game if needed
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    // UI utility methods
    public void SetApiKey(string newKey) => apiKey = newKey;
    public void SetProjectHash(string newHash) => projectHash = newHash;
    public void SetLocalJsonFilePath(string newPath) => localJsonFilePath = newPath;
} 