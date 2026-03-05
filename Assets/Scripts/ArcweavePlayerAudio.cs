using Arcweave;
using Arcweave.Project;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles playback of audio assets associated with Arcweave elements.
/// Subscribes to ArcweavePlayer events and manages AudioSources for each element's audio assets.
/// </summary>
public class ArcweavePlayerAudio : MonoBehaviour
{
    [Header("References")]
    /// <summary>
    /// Reference to the ArcweavePlayer controlling the flow of elements.
    /// </summary>
    public ArcweavePlayer player;
    public DialogueTrigger dialogueTrigger;
    [Header("Debug Settings")]
    /// <summary>
    /// Enables debug logging for audio playback and event subscription.
    /// </summary>
    public bool debugMode = false;

    private bool isInitialized = false;
    /// <summary>
    /// List of AudioSources used to play audio assets for the current element.
    /// </summary>
    private List<AudioSource> audioSources;
    /// <summary>
    /// The currently active Arcweave element.
    /// </summary>
    private Element currentElement = null;
    /// <summary>
    /// Ensures a valid ArcweavePlayer reference is assigned.
    /// </summary>

    void Awake()
    {
        // Ensure we have a valid player reference
        if (player == null)
        {
            player = GetComponent<ArcweavePlayer>();
            if (player == null)
            {
                player = FindAnyObjectByType<ArcweavePlayer>();
                if (player == null)
                {
                    Debug.LogError("ArcweavePlayer not found. Please assign in the inspector.");
                }
            }
        }

        if (dialogueTrigger == null)
        {
            // find dialouge trigger in this game object
            dialogueTrigger = GetComponent<DialogueTrigger>();

            if (dialogueTrigger == null)
            {
                Debug.LogError($"DialogueTrigger not found on {gameObject.name}. Please assign in the inspector.");
            }

        }
    }

    /// <summary>
    /// Initializes event subscriptions when enabled.
    /// </summary>
    void OnEnable()
    {
        Initialize();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Initializes the audio system and subscribes to ArcweavePlayer events.
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;

        // Subscribe to Arcweave events
        SubscribeToEvents();

        isInitialized = true;

        if (debugMode)
        {
            Debug.Log($"{this.GetType().Name} initialized");
        }
    }

    /// <summary>
    /// Subscribes to ArcweavePlayer events for element entry.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (player != null)
        {
            // Unsubscribe first to prevent duplicate subscriptions
            UnsubscribeFromEvents();

            player.onElementEnter += OnElementEnter;

            ArcweaveAudioManager audioManager = ArcweaveAudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.OnAudioClipStop += StopAudioClip;
            }

            if (debugMode)
            {
                Debug.Log("Subscribed to ArcweavePlayer events");
            }
        }
        else
        {
            Debug.LogWarning("Cannot subscribe to ArcweavePlayer events - player reference is null");
        }
    }

    private void StopAudioClip(AudioClip clip)
    {
        if (audioSources != null)
        {
            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource != null && audioSource.clip == clip)
                {
                    audioSource.Stop();
                }
            }
        }
    }

    /// <summary>
    /// Handles entering a new Arcweave element and plays associated audio assets.
    /// </summary>
    /// <param name="element">The element being entered.</param>
    private void OnElementEnter(Element element)
    {

        if (element == null)
        {
            Debug.LogError("Cannot display null element");
            return;
        }

        if (!dialogueTrigger.IsInDialogue())
        {
            return;
        }

        currentElement = element;

        if (debugMode)
        {
            Debug.Log($"Entering element: {element.Title}");
        }

        TryPlayElementAudio(element);
    }

    private void TryPlayElementAudio(Element element)
    {
        AudioAsset[] audioAssets = element.GetAudioAssets();
        if (audioAssets == null || audioAssets.Length == 0)
        {
            if (debugMode)
            {
                Debug.Log("No audio assets for this element");
            }
            return;
        }

        if (audioSources == null)
        {
            audioSources = new List<AudioSource>();
        }

        // Clear AudioSources if needed
        ClearAudioSources();

        for (int i = 0; i < audioAssets.Length; i++)
        {
            AudioAsset assetInfo = audioAssets[i];
            AudioClip clip = assetInfo.TryGetAudioClip();
            if (clip == null)
            {
                Debug.LogWarning($"ArcweavePlayerAudio: Coudn't fetch clip for asset with index {assetInfo.asset}" +
                $" and name {assetInfo.name}");
                continue;
            }

            if (i >= audioSources.Count)
            {
                audioSources.Add(gameObject.AddComponent<AudioSource>());
            }


            if (assetInfo.mode != AudioAsset.Mode.Stop)
            {
                ConfigureAudioSource(i, assetInfo, clip);

                if (assetInfo.delay > 0)
                {
                    audioSources[i].PlayDelayed(assetInfo.delay);
                }
                else
                {
                    audioSources[i].Play();
                }
            }
            else
            {
                if (ArcweaveAudioManager.Instance != null)
                {
                    ArcweaveAudioManager.Instance.SignalAudioClipStop(clip);
                }
            }

        }

    }

    /// <summary>
    /// Configures and plays an AudioSource for a given audio asset and clip.
    /// </summary>
    /// <param name="audioIndex">Index of the AudioSource in the list.</param>
    /// <param name="assetInfo">Audio asset information.</param>
    /// <param name="clip">AudioClip to play.</param>
    private void ConfigureAudioSource(int audioIndex, AudioAsset assetInfo, AudioClip clip)
    {
        // With the following code to add an index check:
        if (audioSources == null || audioIndex >= audioSources.Count || audioSources[audioIndex] == null)
        {
            Debug.LogError($"AudioSource index {audioIndex} is out of range or null. Creating new AudioSource.");
            return;
        }

        AudioSource audioSource = audioSources[audioIndex];
        audioSource.clip = clip;
        audioSource.volume = assetInfo.volume;
        switch (assetInfo.mode)
        {
            // set the audio source based on the mode
            case AudioAsset.Mode.Once:
                audioSource.loop = false;
                break;
            case AudioAsset.Mode.Loop:
                audioSource.loop = true;
                break;
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 2D sound

    }

    /// <summary>
    /// Stops and resets all AudioSources in the list.
    /// </summary>
    private void ClearAudioSources()
    {
        if (audioSources == null) return;

        foreach (var source in audioSources)
        {
            if (source != null && !source.loop)
            {
                Destroy(source);
            }
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (player != null)
        {
            player.onElementEnter -= OnElementEnter;

            if (debugMode)
            {
                Debug.Log("Unsubscribed from ArcweavePlayer events");
            }
        }

        ArcweaveAudioManager audioManager = ArcweaveAudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.OnAudioClipStop -= StopAudioClip;

            if (debugMode)
            {
                Debug.Log("Unsubscribed from ArcweaveAudioManager events");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("ArcweaveAudioManager is null cannot subsribe to events");
        }
    }

}
