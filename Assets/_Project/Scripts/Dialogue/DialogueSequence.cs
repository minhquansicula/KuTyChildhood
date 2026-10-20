using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSequence", menuName = "KuTy/Dialogue/Dialogue Sequence")]
public sealed class DialogueSequence : ScriptableObject
{
    [Serializable]
    public sealed class DialogueLine
    {
        [Tooltip("Stable authoring ID used to match generated voice files and manifest entries.")]
        public string id;
        public string speakerId;
        [TextArea(2, 5)] public string text;
        [TextArea(2, 5)] public string spokenText;
        [Min(.1f)] public float fallbackDuration = 3f;
        [Min(0f)] public float postDelay;
        public AudioClip voiceClip;
        public bool slam;
        public bool silentBeat;

        public string EffectiveSpokenText => string.IsNullOrWhiteSpace(spokenText) ? text : spokenText;

        public float ResolveDuration()
        {
            float contentDuration = voiceClip != null ? voiceClip.length : Mathf.Max(.1f, fallbackDuration);
            return contentDuration + Mathf.Max(0f, postDelay);
        }

        public string TextHash => CalculateStableTextHash(speakerId, text, EffectiveSpokenText);
    }

    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public IReadOnlyList<DialogueLine> Lines => lines;
    public int Count => lines != null ? lines.Count : 0;

    public DialogueLine GetLine(int index)
    {
        if (lines == null || index < 0 || index >= lines.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return lines[index];
    }

    public List<string> CollectValidationIssues(bool requireVoiceClips)
    {
        var issues = new List<string>();
        if (lines == null || lines.Count == 0)
        {
            issues.Add("Sequence has no dialogue lines.");
            return issues;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < lines.Count; i++)
        {
            DialogueLine line = lines[i];
            string label = $"Line {i + 1}";
            if (line == null)
            {
                issues.Add(label + " is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(line.id)) issues.Add(label + " has no stable ID.");
            else if (!ids.Add(line.id)) issues.Add(label + $" duplicates ID '{line.id}'.");

            bool hasVisibleText = !string.IsNullOrWhiteSpace(line.text);
            if (!line.silentBeat && !hasVisibleText) issues.Add(label + " has no text and is not marked as a silent beat.");
            if (line.silentBeat && hasVisibleText) issues.Add(label + " is a silent beat but still has visible text.");
            if (line.fallbackDuration <= 0f) issues.Add(label + " has a non-positive fallback duration.");
            if (line.postDelay < 0f) issues.Add(label + " has a negative post delay.");
            if (requireVoiceClips && !line.silentBeat && line.voiceClip == null)
                issues.Add(label + $" ('{line.id}') is missing its voice clip.");
        }
        return issues;
    }

    public static string CalculateStableTextHash(string speaker, string text, string spokenText)
    {
        // FNV-1a is deterministic across editor sessions and platforms, unlike string.GetHashCode().
        unchecked
        {
            uint hash = 2166136261;
            string value = (speaker ?? string.Empty) + "\n" + (text ?? string.Empty) + "\n" + (spokenText ?? string.Empty);
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619;
            }
            return hash.ToString("x8");
        }
    }

#if UNITY_EDITOR
    public void ReplaceLinesForEditor(IEnumerable<DialogueLine> authoredLines)
    {
        lines = authoredLines != null ? new List<DialogueLine>(authoredLines) : new List<DialogueLine>();
    }
#endif
}
