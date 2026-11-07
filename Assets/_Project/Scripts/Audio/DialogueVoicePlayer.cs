using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class DialogueVoicePlayer : MonoBehaviour
{
    private static readonly HashSet<DialogueVoicePlayer> playing = new HashSet<DialogueVoicePlayer>();
    public static bool AnyVoicePlaying
    {
        get
        {
            foreach (var player in playing)
                if (player && player.isActiveAndEnabled && !player.muted && player.IsPlaying) return true;
            return false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlaybackRegistry() => playing.Clear();
    [SerializeField] private AudioSource source;
    [SerializeField, Range(0f, 1f)] private float volume = .85f;
    [SerializeField] private bool muted;

    public AudioSource Source => source;
    public bool IsPlaying => source != null && source.isPlaying;
    public bool IsPaused { get; private set; }
    public bool Muted => muted;
    public int PlaybackToken { get; private set; }

    private void Awake()
    {
        EnsureSource();
        ApplySettings();
    }

    private void Reset()
    {
        EnsureSource();
        ApplySettings();
    }

    public int Play(AudioClip clip)
    {
        EnsureSource();
        PlaybackToken++;
        IsPaused = false;
        source.Stop();
        source.clip = clip;
        ApplySettings();
        if (clip != null) { playing.Add(this); source.Play(); }
        return PlaybackToken;
    }

    public void Pause()
    {
        if (source == null || IsPaused || source.clip == null) return;
        source.Pause();
        IsPaused = true;
    }

    public void Resume()
    {
        if (source == null || !IsPaused || source.clip == null) return;
        source.UnPause();
        IsPaused = false;
    }

    public void Stop()
    {
        playing.Remove(this);
        PlaybackToken++;
        IsPaused = false;
        if (source == null) return;
        source.Stop();
        source.clip = null;
    }

    public void SetMuted(bool value)
    {
        muted = value;
        ApplySettings();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        ApplySettings();
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(AudioSource audioSource, float initialVolume = .85f)
    {
        source = audioSource;
        volume = Mathf.Clamp01(initialVolume);
        ApplySettings();
    }
#endif

    private void EnsureSource()
    {
        if (source == null) source = GetComponent<AudioSource>();
    }

    private void ApplySettings()
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.pitch = 1f;
        source.volume = volume;
        source.mute = muted;
        KuTyAudioMix.Route(source, KuTyAudioBus.Voice);
    }

    private void OnDisable()
    {
        Stop();
    }
}
