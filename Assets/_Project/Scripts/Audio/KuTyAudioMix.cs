using System;
using UnityEngine;
using UnityEngine.Audio;

public enum KuTyAudioBus { Music, SFX, Voice, Ambience }

/// <summary>User gains live in the mixer; source volume remains owned by gameplay/fades.</summary>
public static class KuTyAudioMix
{
    private static AudioMixer mixer;
    private static readonly AudioMixerGroup[] groups = new AudioMixerGroup[4];
    public static AudioMixer Mixer => mixer ? mixer : (mixer = Resources.Load<AudioMixer>("KuTyMix"));

    public static AudioMixerGroup Group(KuTyAudioBus bus)
    {
        int index = (int)bus;
        if (!groups[index] && Mixer)
            foreach (var candidate in Mixer.FindMatchingGroups(bus.ToString()))
                if (candidate.name == bus.ToString()) { groups[index] = candidate; break; }
        return groups[index];
    }

    public static void Route(AudioSource source, KuTyAudioBus bus)
    {
        if (source && Group(bus)) source.outputAudioMixerGroup = Group(bus);
    }

    public static float ToDecibels(float value) => value <= .0001f ? -80f : 20f * Mathf.Log10(Mathf.Clamp01(value));

    // Call in Start or later, never Awake/OnEnable (Unity mixer initialization contract).
    public static void ApplyPreferences()
    {
        AudioListener.volume = GameUiPreferences.MasterVolume;
        if (!Mixer) return;
        foreach (KuTyAudioBus bus in Enum.GetValues(typeof(KuTyAudioBus)))
            Mixer.SetFloat(bus + "Volume", ToDecibels(GameUiPreferences.GetBusVolume(bus)));
    }
}
