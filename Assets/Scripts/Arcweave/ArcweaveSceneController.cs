using UnityEngine;
using Arcweave;
using Arcweave.Project;

/// <summary>
/// Controls scene elements based on Arcweave attributes like time of day and particle effects
/// </summary>
public class ArcweaveSceneController : MonoBehaviour
{
    [Header("Arcweave References")]
    public ArcweavePlayer arcweavePlayer;
    public RuntimeArcweaveImporter arcweaveImporter;
    
    [Header("Component Settings")]
    [Tooltip("The name of the Arcweave component to search for")]
    public string componentName = "SceneSettings";
    
    [Header("Attribute Settings")]
    [Tooltip("The name of the attribute for time (Day/Night)")]
    public string timeAttribute = "Time";
    [Tooltip("The name of the attribute for particle system state")]
    public string particleAttribute = "ParticleState";
    
    [Header("Visual Effects")]
    public Camera sceneCamera;
    public ParticleSystem[] particleSystems;
    
    private bool initialized = false;
    
    private void Start()
    {
        // Initialize references if needed
        if (arcweavePlayer == null)
        {
            arcweavePlayer = FindObjectOfType<ArcweavePlayer>();
            if (arcweavePlayer == null)
            {
                Debug.LogWarning("ArcweavePlayer not found!");
                return;
            }
        }
        
        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                Debug.LogWarning("No camera found for background control!");
            }
        }
        
        // Set up event listeners
        if (arcweaveImporter != null)
        {
            arcweaveImporter.onImportSuccess.AddListener(OnImportSuccess);
        }
        
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish += OnProjectFinish;
        }
        
        // Initialize scene
        UpdateSceneFromArcweave();
        initialized = true;
    }
    
    /// <summary>
    /// Called when the game state changes
    /// </summary>
    public void OnGameStateChanged(GameManager.GameState newState)
    {
        // Update scene elements based on game state
        switch (newState)
        {
            case GameManager.GameState.Gameplay:
                // Update scene when returning to gameplay
                UpdateSceneFromArcweave();
                break;
                
            case GameManager.GameState.Dialogue:
                // Optionally adjust scene for dialogue mode
                break;
                
            case GameManager.GameState.Paused:
                // Optionally adjust scene for paused mode
                break;
        }
        
        Debug.Log($"ArcweaveSceneController: Game state changed to {newState}");
    }
    
    /// <summary>
    /// Called when a new project is successfully imported
    /// </summary>
    private void OnImportSuccess()
    {
        UpdateSceneFromArcweave();
    }
    
    /// <summary>
    /// Called when an Arcweave project finishes
    /// </summary>
    private void OnProjectFinish(Arcweave.Project.Project project)
    {
        UpdateSceneFromArcweave();
    }
    
    /// <summary>
    /// Updates all scene elements from Arcweave attributes
    /// </summary>
    public void UpdateSceneFromArcweave()
    {
        if (arcweavePlayer == null || arcweavePlayer.aw == null || arcweavePlayer.aw.Project == null)
        {
            Debug.LogWarning("Arcweave project not initialized!");
            return;
        }
        
        // Find the scene settings component
        var component = FindComponentByName(componentName);
        if (component == null)
        {
            Debug.LogWarning($"Component '{componentName}' not found!");
            return;
        }
        
        // Update time of day (affects camera background)
        UpdateTimeOfDay(component);
        
        // Update particle systems
        UpdateParticleSystems(component);
    }
    
    /// <summary>
    /// Updates camera background based on time of day attribute
    /// </summary>
    private void UpdateTimeOfDay(Arcweave.Project.Component component)
    {
        if (sceneCamera == null) return;
        
        var timeAttr = FindAttributeByName(component, timeAttribute);
        if (timeAttr == null)
        {
            Debug.LogWarning($"Attribute '{timeAttribute}' not found!");
            return;
        }
        
        string timeValue = timeAttr.data?.ToString();
        if (string.IsNullOrEmpty(timeValue))
        {
            Debug.LogWarning($"Time value is empty or invalid!");
            return;
        }
        
        // Set background based on time value
        switch (timeValue.ToLower())
        {
            case "day":
                sceneCamera.clearFlags = CameraClearFlags.Skybox;
                Debug.Log("Setting background to Skybox (Day)");
                break;
                
            case "night":
                sceneCamera.clearFlags = CameraClearFlags.SolidColor;
                sceneCamera.backgroundColor = Color.black;
                Debug.Log("Setting background to Solid Color Black (Night)");
                break;
                
            default:
                Debug.LogWarning($"Unknown time value: {timeValue}");
                break;
        }
    }
    
    /// <summary>
    /// Updates particle systems based on particle state attribute
    /// </summary>
    private void UpdateParticleSystems(Arcweave.Project.Component component)
    {
        if (particleSystems == null || particleSystems.Length == 0) return;
        
        var particleStateAttr = FindAttributeByName(component, particleAttribute);
        if (particleStateAttr == null)
        {
            Debug.LogWarning($"Attribute '{particleAttribute}' not found!");
            return;
        }
        
        string particleState = particleStateAttr.data?.ToString();
        if (string.IsNullOrEmpty(particleState))
        {
            Debug.LogWarning($"Particle state value is empty or invalid!");
            return;
        }
        
        // Check for specific weather values
        bool shouldPlay = false;
        
        switch (particleState.ToLower())
        {
            case "rain":
                shouldPlay = true;
                Debug.Log("Weather set to Rain - Activating particle systems");
                break;
                
            case "clear":
                shouldPlay = false;
                Debug.Log("Weather set to Clear - Deactivating particle systems");
                break;
                
            case "true":
            case "1":
                shouldPlay = true;
                Debug.Log("Particle systems activated (legacy value)");
                break;
                
            case "false":
            case "0":
                shouldPlay = false;
                Debug.Log("Particle systems deactivated (legacy value)");
                break;
                
            default:
                Debug.LogWarning($"Unknown particle state value: {particleState}");
                return;
        }
        
        // Apply to all particle systems
        foreach (var ps in particleSystems)
        {
            if (ps == null) continue;
            
            if (shouldPlay)
            {
                if (!ps.isPlaying) ps.Play();
            }
            else
            {
                if (ps.isPlaying) ps.Stop();
            }
        }
        
        Debug.Log($"Particle systems set to: {(shouldPlay ? "Playing" : "Stopped")}");
    }
    
    /// <summary>
    /// Finds an Arcweave component by name
    /// </summary>
    private Arcweave.Project.Component FindComponentByName(string name)
    {
        if (arcweavePlayer?.aw?.Project == null) return null;
        
        foreach (var component in arcweavePlayer.aw.Project.components)
        {
            if (component != null && component.Name == name)
            {
                return component;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Finds an attribute by name in the given component
    /// </summary>
    private Arcweave.Project.Attribute FindAttributeByName(Arcweave.Project.Component component, string attributeName)
    {
        if (component == null || component.Attributes == null) return null;
        
        foreach (var attribute in component.Attributes)
        {
            if (attribute != null && attribute.Name == attributeName)
            {
                return attribute;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Clean up event listeners
    /// </summary>
    private void OnDestroy()
    {
        if (arcweavePlayer != null)
        {
            arcweavePlayer.onProjectFinish -= OnProjectFinish;
        }
        
        if (arcweaveImporter != null)
        {
            arcweaveImporter.onImportSuccess.RemoveListener(OnImportSuccess);
        }
    }
} 