using UnityEngine;
using UnityEngine.Events;
using System.IO;
using Arcweave;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Image Settings")]
    public bool useImageLoadingProxy = true;
    public bool preloadImages = true;
    
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
        Debug.Log("RuntimeArcweaveImporter starting... Loading project from local path.");
        
        // Load directly from local JSON file
        StartCoroutine(LoadLocalProjectWithDelay());
    }
    
    /// <summary>
    /// Loads the local project with a small delay to ensure everything is initialized
    /// </summary>
    private IEnumerator LoadLocalProjectWithDelay()
    {
        // Wait a moment to allow other components to initialize
        yield return new WaitForSeconds(0.5f);
        
        // Try to load the local JSON file
        ImportFromLocalFile();
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
        
        // Clear existing image cache and create default folders
        if (imageLoader != null)
        {
            imageLoader.ClearCache();
        }
        
        // Ensure default folders exist
        CreateDefaultImageFolders();
        
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
            
            // Create a temporary TextAsset with the JSON content
            TextAsset tempJsonAsset = new TextAsset(jsonContent);
            
            // Store the original TextAsset and import source
            TextAsset originalJsonFile = arcweaveAsset.projectJsonFile;
            ArcweaveProjectAsset.ImportSource originalSource = arcweaveAsset.importSource;
            
            // Set up for import
            arcweaveAsset.importSource = ArcweaveProjectAsset.ImportSource.FromJson;
            arcweaveAsset.projectJsonFile = tempJsonAsset;
            
            Debug.Log("Starting import from local JSON file...");
            
            // Import the project
            arcweaveAsset.ImportProject(() => {
                // Restore original settings
                arcweaveAsset.projectJsonFile = originalJsonFile;
                arcweaveAsset.importSource = originalSource;
                
                if (arcweaveAsset.Project != null)
                {
                    Debug.Log("Project imported successfully from local file");
                    
                    // Ensure images are loaded from all possible locations
                    EnsureImagesAreLoaded();
                    
                    // If using the image loading proxy, enable it
                    if (useImageLoadingProxy && imageLoader != null)
                    {
                        imageLoader.InstallImageLoadingHook();
                        Debug.Log("Image loading proxy installed");
                    }
                    
                    // Preload all images if enabled
                    if (preloadImages && imageLoader != null)
                    {
                        imageLoader.PreloadProjectCovers(arcweaveAsset.Project);
                        Debug.Log("Project images preloaded");
                    }
                    
                    // Process project images if they exist in the project
                    ProcessProjectImages(arcweaveAsset.Project);
                    
                    // Disable debug logging after setup
                    if (imageLoader != null)
                    {
                        imageLoader.logDebugInfo = false;
                    }
                    
                    UpdateParticleSystem();
                    UpdateGameState();
                    FinishImport(true);
                }
                else
                {
                    Debug.LogError("Failed to import project - Project is null");
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
    /// Ensures images are loaded from the appropriate paths
    /// </summary>
    private void EnsureImagesAreLoaded()
    {
        if (imageLoader == null || arcweaveAsset == null || arcweaveAsset.Project == null)
            return;
            
        Debug.Log("Setting up image loading paths...");
        
        // Add all possible image locations systematically
        
        // 1. Main project folder path for local images (most common location)
        string projectFolderPath = Path.Combine(
            Path.GetDirectoryName(Application.dataPath),
            "arcweave/images"
        );
        AddImageSearchPath(projectFolderPath);
        
        // 2. Resources folder inside Assets (Unity's standard location)
        string resourcesPath = Path.Combine(Application.dataPath, "Resources");
        AddImageSearchPath(resourcesPath);
        
        // 3. Arcweave folder inside Resources (common Arcweave setup)
        string arcweaveResourcesPath = Path.Combine(Application.dataPath, "Resources/Arcweave");
        AddImageSearchPath(arcweaveResourcesPath);
        
        // 4. Alternative image folder commonly used
        string altImagePath = Path.Combine(
            Path.GetDirectoryName(Application.dataPath),
            "arcweave_images"
        );
        AddImageSearchPath(altImagePath);
        
        // 5. Application's persistent data path for runtime-added images
        string persistentDataPath = Path.Combine(Application.persistentDataPath, "arcweave/images");
        AddImageSearchPath(persistentDataPath);
        
        // Enable debug logging temporarily to track image loading
        if (imageLoader != null)
        {
            imageLoader.logDebugInfo = true;
        }
    }
    
    /// <summary>
    /// Helper method to safely add an image search path
    /// </summary>
    private void AddImageSearchPath(string path)
    {
        if (imageLoader == null) return;
        
        // Check if directory exists before adding
        if (Directory.Exists(path))
        {
            imageLoader.AddSearchPath(path);
            Debug.Log($"Added image search path: {path}");
            
            // Log the files in this directory to help debug
            try {
                var files = Directory.GetFiles(path);
                if (files.Length > 0)
                {
                    Debug.Log($"Found {files.Length} files in {path}:");
                    foreach (var file in files)
                    {
                        if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                        {
                            Debug.Log($"  - {Path.GetFileName(file)}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"Directory {path} exists but contains no files.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error checking files in {path}: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Creates default image folders if they don't exist
    /// </summary>
    public void CreateDefaultImageFolders()
    {
        try
        {
            // Create main arcweave/images folder
            string projectFolderPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "arcweave/images"
            );
            
            if (!Directory.Exists(projectFolderPath))
            {
                Directory.CreateDirectory(projectFolderPath);
                Debug.Log($"Created default image folder: {projectFolderPath}");
            }
            
            // Create Resources folder for Arcweave if it doesn't exist
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                Debug.Log($"Created Resources folder: {resourcesPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating image folders: {e.Message}");
        }
    }
    
    /// <summary>
    /// Process project images to ensure they're available
    /// </summary>
    private void ProcessProjectImages(Arcweave.Project.Project project)
    {
        if (project == null || imageLoader == null)
            return;
            
        Debug.Log("Processing project images...");
        
        int processedCount = 0;
        int missingCount = 0;
        List<string> missingImages = new List<string>();
        
        // Process element images
        if (project.boards != null)
        {
            foreach (var board in project.boards)
            {
                if (board != null && board.Nodes != null)
                {
                    foreach (var node in board.Nodes)
                    {
                        if (node is Arcweave.Project.Element element && element.cover != null && 
                            !string.IsNullOrEmpty(element.cover.filePath))
                        {
                            string imageName = Path.GetFileName(element.cover.filePath);
                            Debug.Log($"Processing image: {imageName} for element {element.Title}");
                            
                            var image = imageLoader.GetCoverImage(element.cover);
                            if (image != null)
                            {
                                processedCount++;
                                Debug.Log($"Successfully loaded image: {imageName}");
                            }
                            else
                            {
                                missingCount++;
                                missingImages.Add(imageName);
                                Debug.LogWarning($"Could not find image: {imageName} for element {element.Title}");
                            }
                        }
                    }
                }
            }
        }
        
        // Check if we found all images
        if (missingCount > 0)
        {
            Debug.LogWarning($"Missing {missingCount} images. Please place them in one of the search directories:");
            foreach (var missingImage in missingImages)
            {
                Debug.LogWarning($"  - Missing image: {missingImage}");
            }
            
            // Suggest folder locations
            Debug.Log("Images should be placed in one of these locations:");
            Debug.Log($"1. {Path.Combine(Path.GetDirectoryName(Application.dataPath), "arcweave/images")}");
            Debug.Log($"2. {Path.Combine(Application.dataPath, "Resources")}");
        }
        
        Debug.Log($"Processed {processedCount} project images. Missing: {missingCount}");
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