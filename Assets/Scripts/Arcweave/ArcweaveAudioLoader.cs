using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles loading Arcweave audio files from the build folder at runtime.
/// Mirrors ArcweaveImageLoader: checks Resources first, then [Build]/arcweave/resources/.
/// </summary>
public class ArcweaveAudioLoader : MonoBehaviour
{
    private static ArcweaveAudioLoader _instance;
    public static ArcweaveAudioLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ArcweaveAudioLoader");
                _instance = go.AddComponent<ArcweaveAudioLoader>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public bool logDebugInfo = false;

    private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

    private static readonly string[] SupportedExtensions = { ".mp3", ".wav", ".ogg", ".aiff", ".aif" };

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Loads an AudioClip by asset name. Checks Resources first, then arcweave/resources/.
    /// Invoke via StartCoroutine. Result delivered through the onLoaded callback.
    /// </summary>
    public IEnumerator LoadAudioClip(string assetName, Action<AudioClip> onLoaded)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            onLoaded?.Invoke(null);
            yield break;
        }

        string key = Path.GetFileNameWithoutExtension(assetName);

        if (_cache.TryGetValue(key, out AudioClip cached))
        {
            onLoaded?.Invoke(cached);
            yield break;
        }

        // Try Unity Resources folder first
        AudioClip clip = Resources.Load<AudioClip>(key);
        if (clip != null)
        {
            _cache[key] = clip;
            if (logDebugInfo) Debug.Log($"ArcweaveAudioLoader: Loaded '{key}' from Resources");
            onLoaded?.Invoke(clip);
            yield break;
        }

        // Try arcweave/resources/ in the build folder
        string folderPath = GetResourcesFolderPath();
        string filePath = FindAudioFile(folderPath, assetName);

        if (filePath == null)
        {
            if (logDebugInfo) Debug.Log($"ArcweaveAudioLoader: '{key}' not found in arcweave/resources/");
            onLoaded?.Invoke(null);
            yield break;
        }

        string uri = "file://" + filePath.Replace("\\", "/");
        AudioType audioType = GetAudioType(filePath);

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            clip = DownloadHandlerAudioClip.GetContent(request);
            clip.name = key;
            _cache[key] = clip;
            if (logDebugInfo) Debug.Log($"ArcweaveAudioLoader: Loaded '{key}' from arcweave/resources/");
            onLoaded?.Invoke(clip);
        }
        else
        {
            Debug.LogWarning($"ArcweaveAudioLoader: Failed to load '{key}': {request.error}");
            onLoaded?.Invoke(null);
        }
    }

    private string FindAudioFile(string folderPath, string assetName)
    {
        if (!Directory.Exists(folderPath)) return null;

        // Try exact filename first
        string exact = Path.Combine(folderPath, assetName);
        if (File.Exists(exact)) return exact;

        // Try appending common extensions
        string nameWithoutExt = Path.GetFileNameWithoutExtension(assetName);
        foreach (string ext in SupportedExtensions)
        {
            string candidate = Path.Combine(folderPath, nameWithoutExt + ext);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    private static string GetResourcesFolderPath()
    {
        string basePath = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(basePath, "arcweave", "resources");
    }

    private static AudioType GetAudioType(string filePath)
    {
        return Path.GetExtension(filePath).ToLower() switch
        {
            ".mp3" => AudioType.MPEG,
            ".ogg" => AudioType.OGGVORBIS,
            ".wav" => AudioType.WAV,
            ".aiff" or ".aif" => AudioType.AIFF,
            _ => AudioType.UNKNOWN
        };
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
