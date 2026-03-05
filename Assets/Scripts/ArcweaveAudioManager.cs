using System;
using UnityEngine;

public class ArcweaveAudioManager : MonoBehaviour
{
    public static ArcweaveAudioManager Instance { get; private set; }

    public Action<AudioClip> OnAudioClipStop;

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

}
