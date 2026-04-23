using System;
using UnityEngine;

public class ArcweaveAudioManager : MonoBehaviour
{
    public static ArcweaveAudioManager Instance { get; private set; }

    public event Action<AudioClip> OnAudioClipStop;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SignalAudioClipStop(AudioClip clip)
    {
        OnAudioClipStop?.Invoke(clip);
    }
    public void SubscribeToAudioClipStop(Action<AudioClip> callback)
    {
        OnAudioClipStop += callback;
    }

    public void UnsubscribeFromAudioClipStop(Action<AudioClip> callback)
    {
        OnAudioClipStop -= callback;
    }

}
