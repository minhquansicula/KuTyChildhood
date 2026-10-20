using System;
using System.Collections.Generic;
using UnityEngine;

public enum SfxId
{
    None = 0,
    UIConfirm = 10,
    UIBack = 11,
    UIInvalid = 12,
    UIHover = 13,
    TaskProgress = 20,
    TaskComplete = 21,
    WaterSplash = 30,
    DishClean = 31,
    MarbleShoot = 40,
    MarbleHit = 41,
    MarbleRoll = 42,
    Purchase = 50,
    PurchaseFail = 51,
    MemoryCollected = 60,
    AllMemoriesComplete = 61
}

public enum SfxCategory
{
    Gameplay,
    UI
}

[Serializable]
public sealed class SfxCue
{
    public SfxId id;
    public SfxCategory category;
    public AudioClip[] variants = Array.Empty<AudioClip>();
    [Range(0f, 1f)] public float volume = 1f;
    [Range(.5f, 1.5f)] public float pitchMin = 1f;
    [Range(.5f, 1.5f)] public float pitchMax = 1f;
    [Min(0f)] public float cooldownSeconds = .05f;
    public bool avoidImmediateRepeat = true;

    public AudioClip GetVariant(int index)
    {
        return variants != null && index >= 0 && index < variants.Length ? variants[index] : null;
    }
}

[CreateAssetMenu(fileName = "SfxCatalog", menuName = "KuTy/Audio/SFX Catalog")]
public sealed class SfxCatalog : ScriptableObject
{
    [SerializeField] private List<SfxCue> cues = new List<SfxCue>();
    private Dictionary<SfxId, SfxCue> lookup;

    public IReadOnlyList<SfxCue> Cues => cues;
    public int Count => cues != null ? cues.Count : 0;

    public bool TryGetCue(SfxId id, out SfxCue cue)
    {
        EnsureLookup();
        return lookup.TryGetValue(id, out cue);
    }

    public List<string> CollectValidationIssues(bool requireClips)
    {
        var issues = new List<string>();
        if (cues == null || cues.Count == 0)
        {
            issues.Add("Catalog has no SFX cues.");
            return issues;
        }

        var ids = new HashSet<SfxId>();
        for (int i = 0; i < cues.Count; i++)
        {
            SfxCue cue = cues[i];
            string label = $"Cue {i + 1}";
            if (cue == null)
            {
                issues.Add(label + " is null.");
                continue;
            }
            if (cue.id == SfxId.None) issues.Add(label + " uses SfxId.None.");
            else if (!ids.Add(cue.id)) issues.Add(label + $" duplicates ID '{cue.id}'.");
            if (cue.volume < 0f || cue.volume > 1f) issues.Add(label + " volume is outside 0..1.");
            if (cue.pitchMin <= 0f || cue.pitchMax <= 0f || cue.pitchMin > cue.pitchMax)
                issues.Add(label + " has an invalid pitch range.");
            if (cue.cooldownSeconds < 0f) issues.Add(label + " has a negative cooldown.");
            if (requireClips && (cue.variants == null || cue.variants.Length == 0))
                issues.Add(label + $" ('{cue.id}') has no clip variants.");
            if (cue.variants != null)
            {
                for (int variant = 0; variant < cue.variants.Length; variant++)
                    if (requireClips && cue.variants[variant] == null)
                        issues.Add(label + $" ('{cue.id}') has a null clip at variant {variant + 1}.");
            }
        }
        return issues;
    }

    private void EnsureLookup()
    {
        if (lookup != null) return;
        lookup = new Dictionary<SfxId, SfxCue>();
        if (cues == null) return;
        for (int i = 0; i < cues.Count; i++)
        {
            SfxCue cue = cues[i];
            if (cue != null && cue.id != SfxId.None && !lookup.ContainsKey(cue.id)) lookup.Add(cue.id, cue);
        }
    }

    private void OnValidate()
    {
        lookup = null;
        if (cues == null) return;
        for (int i = 0; i < cues.Count; i++)
        {
            SfxCue cue = cues[i];
            if (cue == null) continue;
            cue.volume = Mathf.Clamp01(cue.volume);
            cue.pitchMin = Mathf.Clamp(cue.pitchMin, .5f, 1.5f);
            cue.pitchMax = Mathf.Clamp(cue.pitchMax, cue.pitchMin, 1.5f);
            cue.cooldownSeconds = Mathf.Max(0f, cue.cooldownSeconds);
        }
    }

#if UNITY_EDITOR
    public void ReplaceCuesForEditor(IEnumerable<SfxCue> authoredCues)
    {
        cues = authoredCues != null ? new List<SfxCue>(authoredCues) : new List<SfxCue>();
        lookup = null;
    }
#endif
}
