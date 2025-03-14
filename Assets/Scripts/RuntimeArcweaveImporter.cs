using UnityEngine;
using UnityEngine.Events;
using System.IO;
using Arcweave;

/// <summary>
/// Imports Arcweave JSON projects at runtime
/// </summary>
public class RuntimeArcweaveImporter : MonoBehaviour
{
    [Header("References")]
    public ArcweaveProjectAsset arcweaveAsset;
    
    [Header("Web Import Settings")]
    public string apiKey = "";
    public string projectHash = "";
    
    [Header("Local Import Settings")]
    public string localJsonFilePath = "arcweave/project.json";
    
    [Header("Events")]
    public UnityEvent onImportStarted;
    public UnityEvent onImportSuccess;
    public UnityEvent onImportFailed;

    [Header("Effects")]
    public ParticleSystemController particleSystemController;

    // State tracking
    private bool isImporting = false;
    private bool hasLoadedPrepackagedJson = false;
    private ArcweaveImageLoader imageLoader;

    private void Awake()
    {
        imageLoader = ArcweaveImageLoader.Instance;
    }

    private void Start()
    {
        LoadPrepackagedJson();
    }
    
    /// <summary>
    /// Loads the prepackaged JSON file from StreamingAssets
    /// </summary>
    private void LoadPrepackagedJson()
    {
        if (arcweaveAsset == null || hasLoadedPrepackagedJson)
            return;
            
        string jsonPath = Path.Combine(Application.streamingAssetsPath, "arcweave/project.json");
        
        if (!File.Exists(jsonPath))
        {
            Debug.Log("No prepackaged JSON found in StreamingAssets");
            return;
        }
            
        try
        {
            string jsonContent = File.ReadAllText(jsonPath);
            
            if (string.IsNullOrEmpty(jsonContent))
            {
                Debug.LogError("JSON file is empty");
                return;
            }

            arcweaveAsset.importSource = ArcweaveProjectAsset.ImportSource.FromJson;
            arcweaveAsset.MakeProjectFromJson(jsonContent, () => {
                if (arcweaveAsset.Project != null)
                {
                    Debug.Log("Prepackaged project loaded successfully");
                    hasLoadedPrepackagedJson = true;
                    UpdateParticleSystem();
                }
                else
                {
                    Debug.LogError("Failed to load prepackaged project");
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading prepackaged JSON: {e.Message}");
        }
    }
    
    /// <summary>
    /// Imports a project from a local file
    /// </summary>
    public void ImportFromLocalFile()
    {
        if (isImporting || arcweaveAsset == null)
        {
            Debug.LogWarning("Already importing or asset not assigned");
            return;
        }

        isImporting = true;
        onImportStarted?.Invoke();
        
        if (imageLoader != null)
        {
            imageLoader.ClearCache();
        }
        
        string fullPath = Path.Combine(
            Path.GetDirectoryName(Application.dataPath),
            localJsonFilePath
        );
        
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Local JSON file not found at: {fullPath}");
            FinishImport(false);
            return;
        }
        
        try
        {
            string jsonContent = File.ReadAllText(fullPath);
            
            if (string.IsNullOrEmpty(jsonContent))
            {
                Debug.LogError("JSON file is empty");
                FinishImport(false);
                return;
            }
            
            arcweaveAsset.importSource = ArcweaveProjectAsset.ImportSource.FromJson;
            
            arcweaveAsset.MakeProjectFromJson(jsonContent, () => {
                if (arcweaveAsset.Project != null)
                {
                    Debug.Log("Project imported successfully from local file");
                    UpdateParticleSystem();
                    UpdateGameState();
                    FinishImport(true);
                }
                else
                {
                    Debug.LogError("Failed to import project");
                    FinishImport(false);
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading local JSON: {e.Message}");
            FinishImport(false);
        }
    }
    
    /// <summary>
    /// Imports a project from the Arcweave web API using apiKey and projectHash
    /// </summary>
    public void ImportFromWeb()
    {
        if (isImporting || arcweaveAsset == null)
        {
            Debug.LogWarning("Already importing or asset not assigned");
            return;
        }

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(projectHash))
        {
            Debug.LogError("API Key or Project Hash is empty");
            FinishImport(false);
            return;
        }

        isImporting = true;
        onImportStarted?.Invoke();
        
        if (imageLoader != null)
        {
            imageLoader.ClearCache();
        }
        
        try
        {
            // Set import source to Web
            arcweaveAsset.importSource = ArcweaveProjectAsset.ImportSource.FromWeb;
            arcweaveAsset.userAPIKey = apiKey;
            arcweaveAsset.projectHash = projectHash;
            
            Debug.Log($"Starting web import with API Key: {apiKey.Substring(0, 3)}... and Project Hash: {projectHash}");
            
            // Import from web using the correct method
            arcweaveAsset.ImportProject(() => {
                if (arcweaveAsset.Project != null)
                {
                    Debug.Log("Project imported successfully from web");
                    UpdateParticleSystem();
                    UpdateGameState();
                    FinishImport(true);
                }
                else
                {
                    Debug.LogError("Failed to import project from web - Project is null");
                    FinishImport(false);
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during web import: {e.Message}");
            FinishImport(false);
        }
    }

    /// <summary>
    /// Finishes the import process and triggers the appropriate event
    /// </summary>
    private void FinishImport(bool success)
    {
        isImporting = false;
        
        if (success)
            onImportSuccess?.Invoke();
        else
            onImportFailed?.Invoke();
    }

    /// <summary>
    /// Updates particle system effects based on the new project
    /// </summary>
    private void UpdateParticleSystem()
    {
        if (particleSystemController != null)
        {
            particleSystemController.UpdateParticleSystem();
        }
    }
    
    /// <summary>
    /// Updates the game state based on the new project
    /// </summary>
    private void UpdateGameState()
    {
        // Update Arcweave player
        var player = FindAnyObjectByType<ArcweavePlayer>();
        if (player != null)
        {
            player.ResetVariables();
            player.aw = arcweaveAsset;
        }
        
        // Update variable-based visuals
        var variableEvents = FindAnyObjectByType<ArcweaveVariableEvents>();
        if (variableEvents != null)
        {
            variableEvents.ResetObjectActivation();
            variableEvents.UpdateSliderColor();
            variableEvents.UpdateHealthFromVariable();
            variableEvents.UpdateObjectActivation();
        }
        
        // Note: We don't call ResumeGame here anymore
        // Let the ArcweaveImporterUI handle closing the panel and resuming the game
        // This prevents conflicts between different components trying to control the game state
    }
    
    /// <summary>
    /// Sets the API key for web imports
    /// </summary>
    public void SetApiKey(string key)
    {
        apiKey = key;
        
        // Update the asset property as well
        if (arcweaveAsset != null)
        {
            arcweaveAsset.userAPIKey = key;
        }
    }
    
    /// <summary>
    /// Sets the project hash for web imports
    /// </summary>
    public void SetProjectHash(string hash)
    {
        projectHash = hash;
        
        // Update the asset property as well
        if (arcweaveAsset != null)
        {
            arcweaveAsset.projectHash = hash;
        }
    }
    
    /// <summary>
    /// Sets the local JSON file path
    /// </summary>
    public void SetLocalJsonFilePath(string path)
    {
        localJsonFilePath = path;
    }
    
    /// <summary>
    /// Returns a user-friendly path for placing the JSON file
    /// </summary>
    public string GetUserFriendlyPath()
    {
        string basePath = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(basePath, localJsonFilePath);
    }
} 