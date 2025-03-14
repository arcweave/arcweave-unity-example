using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Handles loading Arcweave images from different sources:
/// 1. Resources folder (original behavior)
/// 2. StreamingAssets/arcweave/images/ (for prepackaged images)
/// 3. [Game Folder]/arcweave/images/ (for user-added images)
/// </summary>
public class ArcweaveImageLoader : MonoBehaviour
{
    // Singleton instance
    private static ArcweaveImageLoader _instance;
    public static ArcweaveImageLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ArcweaveImageLoader");
                _instance = go.AddComponent<ArcweaveImageLoader>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Image Settings")]
    public bool logDebugInfo = false;
    public bool enableCache = true;
    
    // Custom folder paths (relative to Application.dataPath)
    public string customImageFolderPath = "";

    // Cache for loaded textures to avoid reloading the same image multiple times
    private Dictionary<string, Texture2D> _imageCache = new Dictionary<string, Texture2D>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (logDebugInfo)
        {
            LogFolderPaths();
        }
    }
    
    /// <summary>
    /// Log the paths being checked for images
    /// </summary>
    private void LogFolderPaths()
    {
        Debug.Log($"ArcweaveImageLoader: Looking for images in:");
        Debug.Log($"- Resources Folder");
        Debug.Log($"- StreamingAssets Folder: {Path.Combine(Application.streamingAssetsPath, "arcweave/images")}");
        
        string buildFolderPath = Application.isEditor ? 
            Application.dataPath.Replace("/Assets", "") : 
            Path.GetDirectoryName(Application.dataPath);
            
        Debug.Log($"- Game Folder: {Path.Combine(buildFolderPath, "arcweave/images")}");
        
        if (!string.IsNullOrEmpty(customImageFolderPath))
        {
            Debug.Log($"- Custom Folder: {Path.Combine(Application.dataPath, customImageFolderPath)}");
        }
    }

    /// <summary>
    /// Loads an image from various sources based on the file path.
    /// </summary>
    /// <param name="filePath">The original file path from Arcweave</param>
    /// <returns>The loaded texture or null if not found</returns>
    public Texture2D LoadImage(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogWarning("ArcweaveImageLoader: Empty file path provided");
            return null;
        }

        string imageName = Path.GetFileNameWithoutExtension(filePath);
        string fileName = Path.GetFileName(filePath);

        // Check if the image is already cached
        if (enableCache && _imageCache.TryGetValue(imageName, out Texture2D cachedTexture))
        {
            if (logDebugInfo) Debug.Log($"ArcweaveImageLoader: Using cached image {imageName}");
            return cachedTexture;
        }

        Texture2D texture = null;
        
        // Try various locations to load the image
        texture = TryLoadFromResources(imageName);
        if (texture != null) return CacheAndReturn(imageName, texture);
        
        texture = TryLoadFromStreamingAssets(fileName);
        if (texture != null) return CacheAndReturn(imageName, texture);
        
        texture = TryLoadFromBuildFolder(fileName);
        if (texture != null) return CacheAndReturn(imageName, texture);
        
        texture = TryLoadFromCustomFolder(fileName);
        if (texture != null) return CacheAndReturn(imageName, texture);

        // If we get here, the image wasn't found
        Debug.LogWarning($"ArcweaveImageLoader: Image not found: {imageName}. Tried Resources, StreamingAssets, and other folders.");
        return null;
    }
    
    /// <summary>
    /// Try to load the image from Resources
    /// </summary>
    private Texture2D TryLoadFromResources(string imageName)
    {
        Texture2D texture = Resources.Load<Texture2D>(imageName);
        if (texture != null && logDebugInfo)
        {
            Debug.Log($"ArcweaveImageLoader: Loaded {imageName} from Resources");
        }
        return texture;
    }
    
    /// <summary>
    /// Try to load the image from StreamingAssets
    /// </summary>
    private Texture2D TryLoadFromStreamingAssets(string fileName)
    {
        string streamingAssetsImagePath = Path.Combine(Application.streamingAssetsPath, "arcweave/images", fileName);
        if (File.Exists(streamingAssetsImagePath))
        {
            Texture2D texture = LoadImageFromFile(streamingAssetsImagePath, Path.GetFileNameWithoutExtension(fileName));
            if (texture != null && logDebugInfo)
            {
                Debug.Log($"ArcweaveImageLoader: Loaded {fileName} from StreamingAssets");
            }
            return texture;
        }
        return null;
    }
    
    /// <summary>
    /// Try to load the image from the build folder
    /// </summary>
    private Texture2D TryLoadFromBuildFolder(string fileName)
    {
        string buildFolderPath = Application.isEditor ? 
            Application.dataPath.Replace("/Assets", "") : 
            Path.GetDirectoryName(Application.dataPath);

        string buildImagePath = Path.Combine(buildFolderPath, "arcweave/images", fileName);
        if (File.Exists(buildImagePath))
        {
            Texture2D texture = LoadImageFromFile(buildImagePath, Path.GetFileNameWithoutExtension(fileName));
            if (texture != null && logDebugInfo)
            {
                Debug.Log($"ArcweaveImageLoader: Loaded {fileName} from build folder");
            }
            return texture;
        }
        return null;
    }
    
    /// <summary>
    /// Try to load the image from a custom folder
    /// </summary>
    private Texture2D TryLoadFromCustomFolder(string fileName)
    {
        if (string.IsNullOrEmpty(customImageFolderPath)) return null;
        
        string customPath = Path.Combine(Application.dataPath, customImageFolderPath, fileName);
        if (File.Exists(customPath))
        {
            Texture2D texture = LoadImageFromFile(customPath, Path.GetFileNameWithoutExtension(fileName));
            if (texture != null && logDebugInfo)
            {
                Debug.Log($"ArcweaveImageLoader: Loaded {fileName} from custom folder");
            }
            return texture;
        }
        return null;
    }
    
    /// <summary>
    /// Cache and return a texture
    /// </summary>
    private Texture2D CacheAndReturn(string imageName, Texture2D texture)
    {
        if (enableCache && texture != null)
        {
            _imageCache[imageName] = texture;
        }
        return texture;
    }

    /// <summary>
    /// Loads an image from a file path.
    /// </summary>
    /// <param name="fullPath">The full path to the image file</param>
    /// <param name="imageName">The name to assign to the texture</param>
    /// <returns>The loaded texture or null if loading failed</returns>
    private Texture2D LoadImageFromFile(string fullPath, string imageName)
    {
        try
        {
            byte[] imageData = File.ReadAllBytes(fullPath);
            Texture2D texture = new Texture2D(2, 2);
            texture.name = imageName;

            if (texture.LoadImage(imageData))
            {
                return texture;
            }
            else
            {
                Debug.LogError($"ArcweaveImageLoader: Failed to load image data for {imageName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ArcweaveImageLoader: Error loading image from {fullPath}: {e.Message}");
        }

        return null;
    }

    /// <summary>
    /// Clears the image cache to free up memory.
    /// </summary>
    public void ClearCache()
    {
        if (logDebugInfo) Debug.Log("ArcweaveImageLoader: Clearing image cache");
        _imageCache.Clear();
        Resources.UnloadUnusedAssets();
    }
    
    /// <summary>
    /// Checks if an image exists in any of the supported locations
    /// </summary>
    public bool ImageExists(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        
        string imageName = Path.GetFileNameWithoutExtension(filePath);
        string fileName = Path.GetFileName(filePath);
        
        // Check cache first
        if (enableCache && _imageCache.ContainsKey(imageName))
        {
            return true;
        }
        
        // Check Resources
        if (Resources.Load<Texture2D>(imageName) != null)
        {
            return true;
        }
        
        // Check StreamingAssets
        string streamingAssetsImagePath = Path.Combine(Application.streamingAssetsPath, "arcweave/images", fileName);
        if (File.Exists(streamingAssetsImagePath))
        {
            return true;
        }
        
        // Check build folder
        string buildFolderPath = Application.isEditor ? 
            Application.dataPath.Replace("/Assets", "") : 
            Path.GetDirectoryName(Application.dataPath);
            
        string buildImagePath = Path.Combine(buildFolderPath, "arcweave/images", fileName);
        if (File.Exists(buildImagePath))
        {
            return true;
        }
        
        // Check custom folder
        if (!string.IsNullOrEmpty(customImageFolderPath))
        {
            string customPath = Path.Combine(Application.dataPath, customImageFolderPath, fileName);
            if (File.Exists(customPath))
            {
                return true;
            }
        }
        
        return false;
    }
} 